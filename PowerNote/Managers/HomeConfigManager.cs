using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PowerNote.Managers
{
	public class HomeConfigManager: INotifyPropertyChanged
	{
		#region event
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		#endregion event

		#region Variables
		public const string DirConfigName = "PowerNote";
		private const string ConfigFileName = "MainConfig.json";
		private ObservableCollection<Project> projects = new ObservableCollection<Project>();

		#endregion Variables

		public ObservableCollection<Project> Projects
		{
			get => projects; set
			{
				projects = value;
				OnPropertyChanged();
			}
		}

		public string FullPathConfig
		{
			get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DirConfigName, ConfigFileName);
		}
		public string FullPathDir
		{
			get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DirConfigName);
		}
		public HomeConfigManager()
		{
			if (!Directory.Exists(FullPathDir))
			{
				Directory.CreateDirectory(FullPathDir);
			}
			Deserialize();
		}

		private void Deserialize()
		{
			try
			{
				if(File.Exists(FullPathConfig))
				{
					var json = File.ReadAllText(FullPathConfig);
					Projects = JsonConvert.DeserializeObject<ObservableCollection<Project>>(json);
				}
			}
			catch { }
		}

		public void Serialize()
		{
			try
			{
				string content = JsonConvert.SerializeObject(Projects);
				File.WriteAllText(FullPathConfig, content);
			}
			catch { }
		}

	}

	

}
