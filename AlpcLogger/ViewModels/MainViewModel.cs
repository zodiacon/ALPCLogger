using AlpcLogger.Models;
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
		DispatcherTimer _timer;
		AlpcCapture _capture = new AlpcCapture();
		public ListCollectionView View { get; }

		public IList<AlpcMessageViewModel> Messages => _messages;

		public readonly IUIServices UI;

		public MainViewModel(IUIServices ui) {
			UI = ui;

			Thread.CurrentThread.Priority = ThreadPriority.Highest;

			_timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
			_timer.Tick += _timer_Tick;
			_timer.Start();

			var thread = new Thread(_capture.Start);
			thread.IsBackground = true;
			thread.Priority = ThreadPriority.BelowNormal;
			thread.Start();

			View = (ListCollectionView)CollectionViewSource.GetDefaultView(Messages);
		}

		private void _timer_Tick(object sender, EventArgs e) {
			_timer.Stop();
			var messages = _capture.GetMessagesAndClear();
			var count = _messages.Count;
			foreach(var msg in messages) {
				_messages.Add(new AlpcMessageViewModel(msg, count++));
			}
			_timer.Start();
		}

		public void Dispose() {
			_capture.Dispose();
		}

		private bool _isRunning;

		public bool IsRunning {
			get { return _isRunning; }
			set {
				if(SetProperty(ref _isRunning, value)) {
					OnPropertyChanged(() => SessionState);
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
						View.Filter = null;
					else {
						var words = value.Trim().ToLowerInvariant().Split(_separators, StringSplitOptions.RemoveEmptyEntries);
						View.Filter = obj => {
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
			var finder = new AlpcChainsFinder(Messages.Where(m => View.PassesFilter(m)).Select(m => m.Message).ToList());
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

		public ICommand ClearLogCommand => new DelegateCommand(() => Messages.Clear());

		public DelegateCommandBase SaveFilteredCommand => new DelegateCommand(() => {
			if(View.Count == 0)
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
			_timer.Stop();

			try {
				var config = new CsvConfiguration {
					IgnorePrivateAccessor = true,
				};

				using(var writer = new StreamWriter(filename)) {
					var csvWriter = new CsvWriter(writer, config);
					csvWriter.WriteHeader<AlpcMessageViewModel>();
					foreach(var msg in _messages)
						if(all || View.Contains(msg))
							csvWriter.WriteRecord(msg);
				}
			}
			catch(Exception ex) {
				UI.MessageBoxService.ShowMessage(ex.Message, App.Name);
			}
			finally {
				_timer.Start();
			}
		}
	}
}
