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
					Projects = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<Project>>(json);
				}
					
			}
			catch { }
		}

		public void Serialize()
		{
			try
			{
				string content = Newtonsoft.Json.JsonConvert.SerializeObject(Projects);
				File.WriteAllText(FullPathConfig, content);
			}
			catch { }
		}

	}

	public class Project : INotifyPropertyChanged
	{
		#region event
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		#endregion event

		private ProjectType type = ProjectType.Text;
		private string content = string.Empty;

		public ProjectType Type
		{
			get => type; set
			{
				type = value;
				OnPropertyChanged();
			}
		}
		public string Content
		{
			get => content; set
			{
				content = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ShortContent));
			}
		}

		[JsonIgnore]
		public string ShortContent
		{
			get
			{
				return Type == ProjectType.Text
					? content.Length > 10 ? Content.Substring(0,10) : Content
					: content != string.Empty ? new FileInfo(content).Name : Content;
			}
			set=> OnPropertyChanged();
		}

		public Project()
		{
		}

		public Project(string content)
		{
			Content = content;
		}

		public Project(ProjectType type, string content)
		{
			Type = type;
			Content = content;
		}
	}


	public enum ProjectType
	{
		Text,
		File
	}

}
