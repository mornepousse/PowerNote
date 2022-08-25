using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PowerNote
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		#region event
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		#endregion event

		private string filePath = string.Empty;

		private string txt = "";
		private char[] spacerChars = new char[] { ' ', '\r', '\n', '\a', '\f', '\t', '\v' };
		private Dictionary<string, double> vars = new Dictionary<string, double>();

		public string Txt
		{
			get => txt; set
			{
				txt = value;
				OnPropertyChanged();
			}
		}

		public Dictionary<string, double> Vars
		{
			get => vars; set
			{
				vars = value;
				OnPropertyChanged();
			}
		}

		public string FilePath
		{
			get => filePath; set
			{
				filePath = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(TitleProject));
			}
		}

		public string TitleProject
		{
			get =>FilePath.Length == 0 ? "PowerNote" : string.Format("PowerNote - {0} - {1}",new FileInfo(FilePath).Name , FilePath);
			set => OnPropertyChanged();
		}

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Main_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F1)
			{
				GetVars();

				int cursorPosition = MainTextBox.SelectionStart;
				int selectionLength = MainTextBox.SelectionLength;
				if (cursorPosition == -1 || string.IsNullOrWhiteSpace(Txt))
					return;

				

				string textTmp = GetCalcul(Txt, cursorPosition);

				if (string.IsNullOrWhiteSpace(textTmp)) return;

				List<string> varListTmp = GetVarsByLengthOrder();

				textTmp = ReplaceVars(varListTmp, textTmp);
				try
				{
					object? result = new DataTable().Compute(textTmp.Replace(',', '.'), null);

					Txt = Txt.Insert(cursorPosition, "=" + result);
				}
				catch { }

			}
			if (e.Key == Key.F2)
			{
				ReCalculAll();
			}
		}

		private void ReCalculAll()
		{
			bool isResult = false;
			bool isVar = false;
			List<int> vsStart = new List<int>();
			List<int> vslength = new List<int>();
			for (int i = 0; i < Txt.Length; i++)
			{
				char c = Txt[i];

				if (c == '$' && !isVar)
					isVar = true;
				if (isVar && spacerChars.Contains(c))
					isVar = false;

				if (isResult && !spacerChars.Contains(c))
				{
					vslength[vslength.Count - 1]++;
				}
				else isResult = false;

				if (c == '=' && !isVar)
				{
					isResult = true;
					vsStart.Add(i);
					vslength.Add(1);
				}
			}

			int rmLength = 0; // diff length old to new
			string tmpTxt = Txt;
			for (int i = 0; i < vsStart.Count; i++)
			{
				int s = vsStart[i];
				int l = vslength[i];
				string suf = "";
				string result = "";
				string pref = tmpTxt.Substring(0, s - rmLength);

				int sufStart = s + l - rmLength;
				int sufEnd = (tmpTxt.Length - 1) - (s + l - rmLength);
				if (sufEnd > 0)
					suf = tmpTxt.Substring(sufStart, sufEnd);

				// get calcul and get result

				string calcul = GetCalcul(pref);

				calcul = ReplaceVars(GetVarsByLengthOrder(), calcul);

				try
				{
					result = "=" + (new DataTable().Compute(calcul.Replace(',', '.'), null)).ToString();
				}
				catch { }

				tmpTxt = pref + result + suf;
				rmLength = (l - result.Length) >= 0 ? (l - result.Length) : (result.Length - l);
			}
			Txt = tmpTxt;
		}

		private string ReplaceVars(List<string> varListTmp, string textTmp)
		{
			foreach (string var in varListTmp)
			{
				if (textTmp.Contains(var))
				{
					textTmp = textTmp.Replace(var, Vars[var].ToString());
				}
			}

			return textTmp;
		}

		private string GetCalcul(string text)
		{
			return GetCalcul(text, text.Length);
		}
		private string GetCalcul(string text, int cursorPosition)
		{
			string textTmp = "";
			for (int i = cursorPosition; i > 0; i--)
			{
				char c = text[i - 1];
				if (c == ' ' || c == '\r' || c == '\n')
					break;
				else textTmp = c + textTmp;
			}

			return textTmp;
		}

		private List<string> GetVarsByLengthOrder()
		{
			if (Vars == null || Vars.Count == 0) 
				return new List<string>();

			List<string> varListTmp = new List<string>();
			foreach (KeyValuePair<string, double> item in vars)
			{
				varListTmp.Add(item.Key.ToString());
			}
			varListTmp = varListTmp.OrderBy(v => v.Length).ToList();
			return varListTmp;
		}

		private void GetVars()
		{
			if (string.IsNullOrWhiteSpace(Txt)) return;
			string[]? varlist = Txt.Replace('\r',' ').Replace('\n', ' ').Split('$');
			if (varlist == null) return;
			if (Vars != null) Vars.Clear();
			else Vars = new Dictionary<string, double>();

			foreach (string? v in varlist)
			{
				if(!string.IsNullOrWhiteSpace(v))
				{
					string[]? valList = v.Split('=');
					if (valList != null && valList.Length == 2 && !valList[0].Contains(' '))
					{
						string? varName = valList[0];
						string[]? valtmp = valList[1].Split(' ');
						if (varName != null && valtmp != null && varName.Length >= 1)
						{
							try
							{
								double val = Convert.ToDouble(valtmp[0].Replace('.',','));
								vars.Add(varName, val);
							}
							catch { }
						}
					}
				}
			}
		}

		private void RecalcAllButton_Click(object sender, RoutedEventArgs e)
		{
			ReCalculAll();
		}

		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
			openFileDlg.DefaultExt = ".txt";
			openFileDlg.Filter = "Text documents (.txt)|*.txt";
			Nullable<bool> result = openFileDlg.ShowDialog(); 
			
			if (result == true)
			{
				FilePath = openFileDlg.FileName;
				Txt = File.ReadAllText(FilePath);
			}
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			File.WriteAllText(FilePath, Txt);
		}

		private void NewButton_Click(object sender, RoutedEventArgs e)
		{
			FilePath = "";
			Txt = "";
		}
	}
}
