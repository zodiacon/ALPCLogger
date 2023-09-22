using AlpcLogger.Models;
using AlpcLogger.Views;
using CsvHelper;
using CsvHelper.Configuration;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Zodiacon.WPF;

namespace AlpcLogger.ViewModels {
	class MainViewModel : BindableBase, IDisposable {
		ObservableCollection<AlpcMessageViewModel> _messages = new ObservableCollection<AlpcMessageViewModel>();
		ObservableCollection<AlpcEventViewModel> _events = new ObservableCollection<AlpcEventViewModel>();

		DispatcherTimer _messagesTimer, _eventsTimer;
		AlpcCapture _capture = new AlpcCapture();
		public ListCollectionView MessagesView { get; }
		public ListCollectionView EventsView { get; }

		public IList<AlpcMessageViewModel> Messages => _messages;
		public IList<AlpcEventViewModel> Events => _events;

		public readonly IUIServices UI;

		public MainViewModel(IUIServices ui) {
			UI = ui;

			Thread.CurrentThread.Priority = ThreadPriority.Highest;

			_messagesTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
			_messagesTimer.Tick += _timer_Tick;
			_messagesTimer.Start();

			_eventsTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1.5) };
			_eventsTimer.Tick += _timer2_Tick;
			_eventsTimer.Start();

			var thread = new Thread(_capture.Start);
			thread.IsBackground = true;
			thread.Priority = ThreadPriority.BelowNormal;
			thread.Start();

			MessagesView = (ListCollectionView)CollectionViewSource.GetDefaultView(Messages);
			EventsView = (ListCollectionView)CollectionViewSource.GetDefaultView(Events);
		}

		private void _timer_Tick(object sender, EventArgs e) {
			_messagesTimer.Stop();
			var messages = _capture.GetMessagesAndClear();
			var count = _messages.Count;
			foreach(var msg in messages) {
				_messages.Add(new AlpcMessageViewModel(msg, count++));
			}

			_messagesTimer.Start();
		}

		private void _timer2_Tick(object sender, EventArgs e) {
			_eventsTimer.Stop();
			var events = _capture.GetEventsAndClear();
			var count = events.Count;
			foreach(var evt in events)
				_events.Add(new AlpcEventViewModel(evt, count++));

			_eventsTimer.Start();
		}

		private int _selectedTab = 1;

		public int SelectedTab {
			get { return _selectedTab; }
			set {
				if(SetProperty(ref _selectedTab, value)) {
				}
			}
		}


		public void Dispose() {
			_capture.Dispose();
		}

		private bool _isRunning;

		public bool IsRunning {
			get { return _isRunning; }
			set {
				if(SetProperty(ref _isRunning, value)) {
					RaisePropertyChanged(nameof(SessionState));
				}
			}
		}

		private string _searchText;

		static char[] _separators = new char[] { ';', ',' };

		public string SearchText {
			get { return _searchText; }
			set {
				if(SetProperty(ref _searchText, value)) {
					if(string.IsNullOrWhiteSpace(value))
						MessagesView.Filter = EventsView.Filter = null;
					else {
						var words = value.Trim().ToLowerInvariant().Split(_separators, StringSplitOptions.RemoveEmptyEntries);
						MessagesView.Filter = obj => {
							var msg = (AlpcMessageViewModel)obj;
							var src = msg.SourceProcessName.ToLowerInvariant();
							var tgt = msg.TargetProcessName.ToLowerInvariant();
							int negates = words.Count(w => w[0] == '-');

							foreach(var text in words) {
								string negText;
								if(text[0] == '-' && text.Length > 2 && (src.Contains(negText = text.Substring(1).ToLowerInvariant()) || tgt.Contains(negText)))
									return false;

								if(text[0] != '-' && (src.Contains(text) || tgt.Contains(text)))
									return true;
							}
							return negates == words.Length;
						};
						EventsView.Filter = obj => {
							var msg = (AlpcEventViewModel)obj;
							var src = msg.ProcessName.ToLowerInvariant();
							int negates = words.Count(w => w[0] == '-');

							foreach(var text in words) {
								if(text[0] == '-' && text.Length > 2 && (src.Contains(text.Substring(1).ToLowerInvariant())))
									return false;

								if(text[0] != '-' && src.Contains(text))
									return true;
							}
							return negates == words.Length;
						};
					}
				}
			}
		}

		public string SessionState => IsRunning ? "Running" : "Stopped";

		public ICommand ExitCommand => new DelegateCommand(() => {
			Dispose();
			Application.Current.MainWindow.Close();
		});

		public DelegateCommandBase StartCommand => new DelegateCommand(() => {
			_capture.Run();
			IsRunning = true;
		}, () => !IsRunning).ObservesProperty(() => IsRunning);

		public DelegateCommandBase StopCommand => new DelegateCommand(() => {
			_capture.Pause();
			IsRunning = false;
		}, () => IsRunning).ObservesProperty(() => IsRunning);

		public ICommand FindChainsCommand => new DelegateCommand(() => {
			var finder = new AlpcChainsFinder(Messages.Where(m => MessagesView.PassesFilter(m)).Select(m => m.Message).ToList());
			foreach(var chain in finder.FindAllChains()) {
				var msg1 = chain[0];
				var msg2 = chain[1];
			}

		});

		public DelegateCommandBase SaveAllCommand => new DelegateCommand(() => {
			if(Messages.Count == 0)
				return;

			DoSave(true);
		});

		private AlpcEventViewModel _selectedEvent;

		public AlpcEventViewModel SelectedEvent {
			get { return _selectedEvent; }
			set { SetProperty(ref _selectedEvent, value); }
		}

		public DelegateCommandBase StackCommand => new DelegateCommand(() => {
			var stack = SelectedEvent.Stack;
			var vm = UI.DialogService.CreateDialog<CallStackViewModel, CallStackView>(stack);
			vm.Show();
		}, () => SelectedEvent != null && SelectedTab == 0)
			.ObservesProperty(() => SelectedEvent).ObservesProperty(() => SelectedTab);

		public ICommand ClearLogCommand => new DelegateCommand(() => {
			Messages.Clear();
			Events.Clear();
		});

		public DelegateCommandBase SaveFilteredCommand => new DelegateCommand(() => {
			if(MessagesView.Count == 0)
				return;

			DoSave(false);
		});

		private void DoSave(bool all) {
			var filename = UI.FileDialogService.GetFileForSave("CSV Files (*.csv)|*.csv|All Files|*.*");
			if(filename == null)
				return;

			SaveInternal(filename, all);
		}

		private void SaveInternal(string filename, bool all) {
			_messagesTimer.Stop();

			try {
				var config = new Configuration {
					IncludePrivateMembers = true,
				};

				using(var writer = new StreamWriter(filename)) {
					var csvWriter = new CsvWriter(writer, config);
					if( all ) {
						csvWriter.WriteRecords(_messages);
					} else {
						csvWriter.WriteHeader<AlpcMessageViewModel>();
						csvWriter.NextRecord();
						foreach(var msg in _messages) {
							if(MessagesView.Contains(msg)) {
								csvWriter.WriteRecord(msg);
								csvWriter.NextRecord();
							}
						}
					}
				}
			}
			catch (Exception ex) {
				UI.MessageBoxService.ShowMessage(ex.Message, App.Name);
			}
			finally {
				_messagesTimer.Start();
			}
		}
	}
}
