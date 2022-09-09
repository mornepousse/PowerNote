﻿using Hardcodet.Wpf.TaskbarNotification;
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

			Application.Current.Resources.SetTheme(Theme.Create(Theme.Dark, (Color)ColorConverter.ConvertFromString("#FFFFE400"), (Color)ColorConverter.ConvertFromString("#FFE96800")));

			notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");

			foreach(Project pro in HomeConfig.Projects)
			{
				if(pro.IsOpen)
				{
					new NoteWindow(pro).Show();
				}
			}

		}

		protected override void OnExit(ExitEventArgs e)
		{
			notifyIcon.Dispose();
			base.OnExit(e);
		}


		public static bool IsWindowOpen<T>(string name = "") where T : Window
		{
			return string.IsNullOrEmpty(name)
			   ? Application.Current.Windows.OfType<T>().Any()
			   : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
		}

		public static IEnumerable<T> GetWindows<T>() where T : Window
		{
			return Application.Current.Windows.OfType<T>();
		}

	}
}
