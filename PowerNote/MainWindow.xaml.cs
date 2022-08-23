using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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

		private string txt = "";
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

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Main_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F1)
			{
				GetVars();

				var cursorPosition = MainTextBox.SelectionStart;
				var selectionLength = MainTextBox.SelectionLength;

				string textTmp = "";

				List<string> varListTmp = GetVarsByLengthOrder();

				for (int i = cursorPosition; i > 0; i--)
				{
					char c = Txt[i - 1];
					if (c == ' ' || c == '\r' || c == '\n')
						break;
					else textTmp = c + textTmp;
				}
				if (string.IsNullOrWhiteSpace(textTmp)) return;
				foreach(string var in varListTmp)
				{
					if(textTmp.Contains(var))
					{
						textTmp = textTmp.Replace(var, Vars[var].ToString());
					}
				}

				var result = new DataTable().Compute(textTmp.Replace(',', '.'), null);

				Txt = Txt.Insert(cursorPosition, "=" + result);

			}
		}

		private List<string> GetVarsByLengthOrder()
		{
			List<string> varListTmp = new List<string>();
			foreach (var item in vars)
			{
				varListTmp.Add(item.Key.ToString());
			}
			varListTmp = varListTmp.OrderBy(v => v.Length).ToList();
			return varListTmp;
		}

		private void GetVars()
		{
			var varlist = Txt.Replace('\r',' ').Replace('\n', ' ').Split('$');
			Vars.Clear();

			foreach (var v in varlist)
			{
				if(!string.IsNullOrWhiteSpace(v))
				{
					var valList = v.Split('=');
					if (valList.Length == 2 && !valList[0].Contains(' '))
					{
						var varName = valList[0];
						var valtmp = valList[1].Split(' ');
						if (varName.Length >= 1)
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
	}
}
