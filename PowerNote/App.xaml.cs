using Hardcodet.Wpf.TaskbarNotification;
using MaterialDesignThemes.Wpf;
using PowerNote.Managers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PowerNote
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private TaskbarIcon notifyIcon;
		private static HomeConfigManager homeConfig = new HomeConfigManager();

		public static HomeConfigManager HomeConfig { get => homeConfig; set => homeConfig = value; }

		protected override void OnStartup(StartupEventArgs e)
		{
			HomeConfig = new HomeConfigManager();
			base.OnStartup(e);

			Application.Current.Resources.SetTheme(Theme.Create(Theme.Dark, (Color)ColorConverter.ConvertFromString("#FF0098FF"), (Color)ColorConverter.ConvertFromString("#FF007ACC")));

			notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
		}

		protected override void OnExit(ExitEventArgs e)
		{
			notifyIcon.Dispose();
			base.OnExit(e);
		}

	}
}
