using PowerNote.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PowerNote.Views
{
	public partial class NotifyIconResources
	{

		private void ShowHome(object sender, RoutedEventArgs e)
		{
			if(!App.IsWindowOpen<HomeWindow>())
			{
				Application.Current.MainWindow = new HomeWindow();
				Application.Current.MainWindow.Show();
			}
		}
		private void ShowNewNote(object sender, RoutedEventArgs e)
		{
			App.HomeConfig.Projects.Add(new Project());
			new NoteWindow(App.HomeConfig.Projects[App.HomeConfig.Projects.Count - 1]).Show();
		}
	}
}
