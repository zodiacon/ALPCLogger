using AlpcLogger.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Zodiacon.WPF;

namespace AlpcLogger {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public const string Name = "ALPC Logger";

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			var ui = new UIServicesDefaults();
			var vm = new MainViewModel(ui);
			var win = new MainWindow { DataContext = vm };
			win.Show();
		}
	}
}
