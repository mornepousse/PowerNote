using PowerNote.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PowerNote
{
	/// <summary>
	/// Logique d'interaction pour HomeWindow.xaml
	/// </summary>
	public partial class HomeWindow : Window, INotifyPropertyChanged
	{


		#region event
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		#endregion event

		public HomeConfigManager HomeConfigManager
		{
			get => App.HomeConfig;
			set => OnPropertyChanged();
		}

		public HomeWindow()
		{
			InitializeComponent();
		
		}

		private void Add_Click(object sender, RoutedEventArgs e)
		{
			New();
		}

		private void New()
		{
			HomeConfigManager.Projects.Add(new Project());
			new NoteWindow(HomeConfigManager.Projects[HomeConfigManager.Projects.Count - 1]).Show();
		}

		private void Open_Click(object sender, RoutedEventArgs e)
		{
			Button button = (Button)sender;

			foreach(var w in App.GetWindows<NoteWindow>())
			{
				if(w.NoteProject == (Project)button.DataContext)
				{
					w.Activate();
					return;
				}
			}

			new NoteWindow((Project)button.DataContext).Show();
		}

		private void NewProjectCommand(object sender, ExecutedRoutedEventArgs e)
		{
			New();
		}

		private void Main_Closed(object sender, EventArgs e)
		{
			HomeConfigManager.Serialize();
		}

		private void Remove_Click(object sender, RoutedEventArgs e)
		{
			Button button = (Button)sender;

			Project project = (Project)button.DataContext;

			if(project != null)
			{
				if(MessageBox.Show("Are you sure ?","Remove",MessageBoxButton.YesNo,MessageBoxImage.Question) == MessageBoxResult.Yes) App.HomeConfig.Projects.Remove(project);
			}

		}

		private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ListBox listbox = (ListBox)sender;

			foreach (var w in App.GetWindows<NoteWindow>())
			{
				if (w.NoteProject == (Project)listbox.SelectedValue)
				{
					w.Activate();
					return;
				}
			}

			new NoteWindow((Project)listbox.SelectedValue).Show();
		}

		private void ChangeColor_Click(object sender, RoutedEventArgs e)
		{
			Button button = (Button)sender;
			Project project = (Project) button.DataContext;

			project.Brush = button.Background;


		}

		private void OpenExternal_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
			openFileDlg.DefaultExt = ".txt";
			openFileDlg.Filter = "Text documents (.txt)|*.txt";
			Nullable<bool> result = openFileDlg.ShowDialog();

			if (result == true)
			{
				Project project = new Project(ProjectType.File,openFileDlg.FileName);
				HomeConfigManager.Projects.Add(project);
			}
		}
	}
}
