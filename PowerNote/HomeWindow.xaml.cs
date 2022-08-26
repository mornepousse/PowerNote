using PowerNote.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
			HomeConfigManager.Projects.Add(new Project());
			new NoteWindow(HomeConfigManager.Projects[HomeConfigManager.Projects.Count - 1]).Show();
		}

		private void Open_Click(object sender, RoutedEventArgs e)
		{
			Button button = (Button)sender;
			new NoteWindow((Project)button.DataContext).Show();
		}
	}
}
