using ICSharpCode.AvalonEdit.Highlighting;
using PowerNote.Managers;
using PowerNote.Managers.Window;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Xml;
using ICSharpCode.AvalonEdit;

namespace PowerNote
{
	/// <summary>
	/// Interaction logic for NoteWindow.xaml
	/// </summary>
	public partial class NoteWindow : System.Windows.Window, INotifyPropertyChanged
	{
		#region event
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		#endregion event

		#region Variables

		//private string filePath = string.Empty;
		private char[] spacerChars = new char[] { ' ', '\r', '\n', '\a', '\f', '\t', '\v' };
		private Dictionary<string, double> vars = new Dictionary<string, double>();
		private Project noteProject;
		private int indexToSave = 0;
		private HwndSource _hwndSource;

		#endregion Variables

		#region Properties


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
			get => NoteProject.Type == ProjectType.File ? NoteProject.Content : string.Empty;
			set
			{
				if (NoteProject.Type == ProjectType.File)
					NoteProject.Content = value;

				OnPropertyChanged();
				OnPropertyChanged(nameof(TitleProject));
			}
		}

		public string TitleProject
		{
			get =>FilePath.Length == 0 ? "PowerNote" : string.Format("PowerNote - {0} - {1}",new FileInfo(FilePath).Name , FilePath);
			set => OnPropertyChanged();
		}
		public Project NoteProject
		{
			get => noteProject; set
			{
				noteProject = value;
				OnPropertyChanged();
			}
		}


		#endregion Properties
		IHighlightingDefinition customHighlighting;
		public NoteWindow(Project project)
		{
			NoteProject = project;
			InitializeComponent();

			using (Stream s = typeof(NoteWindow).Assembly.GetManifestResourceStream("PowerNote.CustomHighlighting.xshd"))
			{
				if (s == null)
					throw new InvalidOperationException("Could not find embedded resource");
				using (XmlReader reader = new XmlTextReader(s))
				{
					customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.
						HighlightingLoader.Load(reader, HighlightingManager.Instance);
				}
			}
			// and register it in the HighlightingManager
			HighlightingManager.Instance.RegisterHighlighting("Custom Highlighting", new string[] { ".cool" }, customHighlighting);

			

			//ButtonWindowStateNormal.Visibility = Visibility.Collapsed;

			if (project.Type == ProjectType.File && File.Exists(project.Content))
			{
				try
				{
					MainTextEditor.Text = File.ReadAllText(project.Content);
				}
				catch 
				{
					MessageBox.Show("Oups");
				}
			}
			else MainTextEditor.Text = project.Content;
		}
		public NoteWindow()
		{
			InitializeComponent();
		}

		private void Main_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.F1)
			{
				GetVars();

				int cursorPosition = MainTextEditor.SelectionStart;
				int selectionLength = MainTextEditor.SelectionLength;
				if (cursorPosition == -1 || string.IsNullOrWhiteSpace(MainTextEditor.Text))
					return;

				

				string textTmp = GetCalcul(MainTextEditor.Text, cursorPosition);

				if (string.IsNullOrWhiteSpace(textTmp)) return;

				List<string> varListTmp = GetVarsByLengthOrder();

				textTmp = ReplaceVars(varListTmp, textTmp);
				try
				{
					object? result = new DataTable().Compute(textTmp.Replace(',', '.'), null);

					MainTextEditor.Text = MainTextEditor.Text.Insert(cursorPosition, "=" + result);
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
			for (int i = 0; i < MainTextEditor.Text.Length; i++)
			{
				char c = MainTextEditor.Text[i];

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
			string tmpTxt = MainTextEditor.Text;
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
			MainTextEditor.Text = tmpTxt;
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
			if (string.IsNullOrWhiteSpace(MainTextEditor.Text)) return;
			string[]? varlist = MainTextEditor.Text.Replace('\r',' ').Replace('\n', ' ').Split('$');
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
				MainTextEditor.Text = File.ReadAllText(FilePath);
			}
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			File.WriteAllText(FilePath, MainTextEditor.Text);
		}

		private void NewButton_Click(object sender, RoutedEventArgs e)
		{
			FilePath = "";
			MainTextEditor.Text = "";
		}

		private void MainTextEditor_TextChanged(object sender, EventArgs e)
		{
			
			if (NoteProject.Type == ProjectType.Text)
				NoteProject.Content = MainTextEditor.Text;
			else
			{
				

			}
			indexToSave++;
			if (indexToSave > 6)
			{
				indexToSave = 0;
				App.HomeConfig.Serialize();
			}
		}


		#region WindowResizing
		private void Window_OnSourceInitialized(object sender, EventArgs e)
		{
			// Call for resizing effects
			_hwndSource = (HwndSource)PresentationSource.FromVisual(this);
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

		private void ResizeWindow(ResizeDirection direction)
		{
			SendMessage(_hwndSource.Handle, 0x112, (IntPtr)(61440 + direction), IntPtr.Zero);
		}

		private enum ResizeDirection
		{
			Left = 1,
			Right = 2,
			Top = 3,
			TopLeft = 4,
			TopRight = 5,
			Bottom = 6,
			BottomLeft = 7,
			BottomRight = 8,
		}

		private void Window_OnPreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed)
				Cursor = Cursors.Arrow;
		}

		private void WindowResize_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			var rectangle = (Rectangle)sender;
			if (rectangle == null) return;

			if (WindowStateHelper.IsMaximized) return;

			switch (rectangle.Name)
			{
				case "WindowResizeTop":
					Cursor = Cursors.SizeNS;
					ResizeWindow(ResizeDirection.Top);
					break;
				case "WindowResizeBottom":
					Cursor = Cursors.SizeNS;
					ResizeWindow(ResizeDirection.Bottom);
					break;
				case "WindowResizeLeft":
					Cursor = Cursors.SizeWE;
					ResizeWindow(ResizeDirection.Left);
					break;
				case "WindowResizeRight":
					Cursor = Cursors.SizeWE;
					ResizeWindow(ResizeDirection.Right);
					break;
				case "WindowResizeTopLeft":
					Cursor = Cursors.SizeNWSE;
					ResizeWindow(ResizeDirection.TopLeft);
					break;
				case "WindowResizeTopRight":
					Cursor = Cursors.SizeNESW;
					ResizeWindow(ResizeDirection.TopRight);
					break;
				case "WindowResizeBottomLeft":
					Cursor = Cursors.SizeNESW;
					ResizeWindow(ResizeDirection.BottomLeft);
					break;
				case "WindowResizeBottomRight":
					Cursor = Cursors.SizeNWSE;
					ResizeWindow(ResizeDirection.BottomRight);
					break;
			}
		}

		private void WindowResize_OnMouseMove(object sender, MouseEventArgs e)
		{
			var rectangle = (Rectangle)sender;
			if (rectangle == null) return;

			if (WindowStateHelper.IsMaximized) return;

			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (rectangle.Name)
			{
				case "WindowResizeTop":
					Cursor = Cursors.SizeNS;
					break;
				case "WindowResizeBottom":
					Cursor = Cursors.SizeNS;
					break;
				case "WindowResizeLeft":
					Cursor = Cursors.SizeWE;
					break;
				case "WindowResizeRight":
					Cursor = Cursors.SizeWE;
					break;
				case "WindowResizeTopLeft":
					Cursor = Cursors.SizeNWSE;
					break;
				case "WindowResizeTopRight":
					Cursor = Cursors.SizeNESW;
					break;
				case "WindowResizeBottomLeft":
					Cursor = Cursors.SizeNESW;
					break;
				case "WindowResizeBottomRight":
					Cursor = Cursors.SizeNWSE;
					break;
			}
		}

		private void WindowDraggableArea_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed)
				return;

			if (WindowStateHelper.IsMaximized)
			{
				WindowStateHelper.SetWindowSizeToNormal(this, true);
				ShowMaximumWindowButton();

				DragMove();
			}
			else
			{
				DragMove();
			}

			WindowStateHelper.UpdateLastKnownLocation(Top, Left);
		}


		private void Window_OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (WindowState == WindowState.Maximized)
			{
				WindowStateHelper.SetWindowMaximized(this);
				WindowStateHelper.BlockStateChange = true;

				var screen = ScreenFinder.FindAppropriateScreen(this);
				if (screen != null)
				{
					Top = screen.WorkingArea.Top;
					Left = screen.WorkingArea.Left;
					Width = screen.WorkingArea.Width;
					Height = screen.WorkingArea.Height;
				}

				ShowRestoreDownButton();
			}
			else
			{
				if (WindowStateHelper.BlockStateChange)
				{
					WindowStateHelper.BlockStateChange = false;
					return;
				}

				WindowStateHelper.UpdateLastKnownNormalSize(Width, Height);
				WindowStateHelper.UpdateLastKnownLocation(Top, Left);
			}
		}

		private void ShowRestoreDownButton()
		{
			//ButtonMaximize.Visibility = Visibility.Collapsed;
			//uttonWindowStateNormal.Visibility = Visibility.Visible;
		}

		private void ShowMaximumWindowButton()
		{
			//ButtonWindowStateNormal.Visibility = Visibility.Collapsed;
			//ButtonMaximize.Visibility = Visibility.Visible;
		}

		private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ButtonMinimize_OnClick(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void ButtonRestoreDown_OnClick(object sender, RoutedEventArgs e)
		{
			ShowMaximumWindowButton();
			WindowStateHelper.SetWindowSizeToNormal(this);
		}

		private void ButtonMaximize_OnClick(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Maximized;
			ShowRestoreDownButton();
		}
		#endregion

		private void Main_Loaded(object sender, RoutedEventArgs e)
		{
			MainTextEditor.SyntaxHighlighting = customHighlighting;
		}
	}
}
