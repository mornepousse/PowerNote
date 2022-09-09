using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PowerNote.Managers
{
	public class Project : INotifyPropertyChanged
	{
		#region event
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		#endregion event

		public static char[] SpacerChars = new char[] { ' ', '\r', '\n', '\a', '\f', '\t', '\v' };

		private string title = "";
		private ProjectType type = ProjectType.Text;
		private string content = string.Empty;
		private double top = 100;
		private double left = 100;
		private double width = 400;
		private double height = 450;
		Brush brush = new SolidColorBrush(Color.FromRgb(230, 185, 5));
		private bool isOpen = false;

		private Dictionary<string, object> vars = new Dictionary<string, object>();



		public Dictionary<string, object> Vars
		{
			get => vars;
			set
			{
				vars = value;
				OnPropertyChanged();
			}
		}
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
					? content.Length > 100 ? Content.Substring(0, 100) : Content
					: content != string.Empty ? new FileInfo(content).Name : Content;
			}
			set => OnPropertyChanged();
		}

		public double Top
		{
			get => top; set
			{
				top = value;
				OnPropertyChanged();
			}
		}

		public double Left
		{
			get => left; set
			{
				left = value;
				OnPropertyChanged();
			}
		}

		public string Title
		{
			get => title; set
			{
				title = value;
				OnPropertyChanged();
			}
		}

		public double Width
		{
			get => width; set
			{
				width = value;
				OnPropertyChanged();
			}
		}
		public double Height
		{
			get => height; set
			{
				height = value;
				OnPropertyChanged();
			}
		}

		public Brush Brush
		{
			get => brush; set
			{
				brush = value;
				OnPropertyChanged();
			}
		}

		public bool IsOpen
		{
			get => isOpen; set
			{
				isOpen = value;
				OnPropertyChanged();
			}
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

		public Project(ProjectType type, string content, double top, double left) : this(type, content)
		{
			Top = top;
			Left = left;
		}

		public Project(ProjectType type, string content, double top, double left, string title) : this(type, content, top, left)
		{
			Title = title;
		}

		public Project(ProjectType type, string content, double top, double left, string title, double width, double height) : this(type, content, top, left, title)
		{
			Width = width;
			Height = height;
		}

		public Project(ProjectType type, string content, double top, double left, string title, double width, double height, Brush brush) : this(type, content, top, left, title, width, height)
		{
			Brush = brush;
		}

		public string ReCalculAll(string Text)
		{

			bool isResult = false;
			bool isVar = false;
			bool isVarTab = false;
			List<int> vsStart = new List<int>();
			List<int> vslength = new List<int>();
			for (int i = 0; i < Text.Length; i++)
			{
				char c = Text[i];

				if(isResult && c == '{')
				{
						isVarTab = true;
				}
				else
				{
					if (isVarTab && c == '{')
					{
						isVarTab = false;
					}
				}

				if (c == '$' && !isVar)
					isVar = true;

				if(isResult && isVarTab)
				{
					if(c != '}')
						vslength[vslength.Count - 1]++;
					else
					{
						vslength[vslength.Count - 1]++;
						isResult = false;
						isVarTab = false;
					}
				}
				else
				{
					if (isVar && Project.SpacerChars.Contains(c))
						isVar = false;

					if (isResult && !Project.SpacerChars.Contains(c))
						vslength[vslength.Count - 1]++;
					else isResult = false;

					if (c == '=' && !isVar)
					{
						isResult = true;
						vsStart.Add(i);
						vslength.Add(1);
						if (Text[i + 1] == '{')
						{
							isVarTab = true;
						}
					}
				}

				
			}

			int rmLength = 0; // diff length old to new
			string tmpTxt = Text;
			int lastSubStringStart = 0;
			List<string> preffixList = new List<string>();
			for (int i = 0; i < vsStart.Count; i++)
			{

				int start = vsStart[i]- lastSubStringStart;
				int OldLength = vslength[i];

				string preffix = tmpTxt.Substring(0, start );
				preffixList.Add(preffix);
				tmpTxt = tmpTxt.Substring(start + OldLength);
				lastSubStringStart = start + OldLength;

			}

			string suffix = tmpTxt;


			tmpTxt = "";
			for (int i = 0; i < vsStart.Count; i++)
			{
				string result = "";
				string calculAndParams = GetCalcul(preffixList[i]);
				
				tmpTxt += preffixList[i];

				PCalculs pCalculs = new PCalculs(calculAndParams);

				List<string> calculs = ReplaceVars(GetVarsByLengthOrder(), pCalculs.Calcul);

				try
				{
					result = GetRTextesult(calculs, pCalculs.IsMin, pCalculs.IsMax, pCalculs.Min, pCalculs.Max);
				}
				catch { }
				tmpTxt += result;
				//tmpTxt = preffix + result + suffix;

			}
			tmpTxt += suffix;
			Text = tmpTxt;
			return Text;
		}



		public string GetRTextesult( List<string> cals, bool isMin = false, bool isMax = false, double min = 0, double max = 0)
		{
			string result;
			if (cals.Count == 1)
			{
				result = "=" + (new DataTable().Compute(cals[0].Replace(',', '.'), null)).ToString();
			}
			else
			{
				result = "={";

				foreach (string cal in cals)
				{
					bool view = true;
					object resultTmp = Convert.ToDouble((new DataTable().Compute(cal.Replace(',', '.'), null)));
					if (isMin && (double)resultTmp < min)
					{
						view = false;
					}
					if (isMax && (double)resultTmp > max)
					{
						view = false;
					}

					if (view)
						result += "\n " + cal + "=" + resultTmp.ToString() + "";

				}
				result += "}";
			}

			return result;
		}


		public string GetCalcul(string text)
		{
			return GetCalcul(text, text.Length);
		}
		public string GetCalcul(string text, int cursorPosition)
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
		public List<string> ReplaceVars(List<string> varListTmp, string textsTmp)
		{

			if(varListTmp.Count == 0)
			{
				return new List<string>() { textsTmp };
			}

			List<string> vs = new List<string>();

			foreach(string var in varListTmp)
			{
				if (textsTmp.Contains(var))
				{
					if (Vars[var].GetType() == typeof(double))
					{
						textsTmp = textsTmp.Replace(var, Vars[var].ToString());
					}

				}
			}


			List<string> vsVars = new List<string>();
			List<string> vsVarsTmp = new List<string>();

			foreach (string var in varListTmp)
			{
				if(vsVarsTmp.Count ==0)
				{
					vsVarsTmp = GetCalculByItemTab(textsTmp, var);
				}
				else
				{
					List<string> vsnewVarsTmp = new List<string>();
					foreach (var v in vsVarsTmp)
					{
						foreach (string newVar in GetCalculByItemTab(v, var))
						{
							vsnewVarsTmp.Add(newVar);
						}
					}
					foreach (string newVar in vsnewVarsTmp)
					{
						vsVarsTmp.Add(newVar);
					}
				}
			}

			foreach(var cal in vsVarsTmp)
			{
				bool isContains = false;
				foreach (string var in varListTmp)
				{
					if(cal.Contains(var))
					{
						isContains = true;
						break;
					}
				}
				if(!isContains) vsVars.Add(cal);
			}

			return vsVars;
		}

		private List<string> GetCalculByItemTab(string textsTmp, string var)
		{
			List<string> vsVars = new List<string>();
			if (textsTmp.Contains(var))
			{

				if (Vars[var].GetType() == typeof(List<double>))
				{
					foreach (double var2 in (List<double>)Vars[var])
					{
						vsVars.Add(textsTmp.Replace(var, var2.ToString()));
					}
				}

			}
			return vsVars;
		}

		public List<string> GetVarsByLengthOrder()
		{
			if (Vars == null || Vars.Count == 0)
				return new List<string>();

			List<string> varListTmp = new List<string>();
			foreach (KeyValuePair<string, object> item in vars)
			{
				varListTmp.Add(item.Key.ToString());
			}
			varListTmp = varListTmp.OrderBy(v => v.Length).ToList();
			return varListTmp;
		}

		public string GetVars(string Text)
		{
			if (string.IsNullOrWhiteSpace(Text)) return "";
			string[]? varlist = Text.Split('$');
			if (varlist == null) return "";
			if (Vars != null) Vars.Clear();
			else Vars = new Dictionary<string, object>();

			foreach (string? v in varlist)
			{
				if (!string.IsNullOrWhiteSpace(v))
				{
					string[]? valList = v.Split('=');
					if (valList != null && valList.Length >= 2 && !valList[0].Contains(' '))
					{
						string? varName = valList[0];
						string[]? valtmpList = valList[1].Split(SpacerChars);
						string valtmp = valtmpList[0];
						if (varName != null && valtmp != null && varName.Length >= 1)
						{
							try
							{
								if (valtmp[0] == '[' && valtmp[valtmp.Length - 1] == ']')
								{
									string t = valtmp.Substring(1, valtmp.Length - 2);

									List<double> doubles = new List<double>();

									foreach (string? part in t.Split(','))
									{
										doubles.Add(Convert.ToDouble(part.Replace('.', ',')));
									}
									Vars.Add(varName, doubles);
								}
								else
								{
									double val = Convert.ToDouble(valtmp.Replace('.', ','));
									vars.Add(varName, val);
								}

							}
							catch { }
						}
					}
				}
			}
			return Text;
		}

	}


	public enum ProjectType
	{
		Text,
		File
	}

	public class PCalculs
	{

		private string calcul = "";
		private bool isMin = false;
		private bool isMax = false;
		private double min = 0;
		private double max = 0;

		public string Calcul { get => calcul; set => calcul = value; }
		public bool IsMin { get => isMin; set => isMin = value; }
		public bool IsMax { get => isMax; set => isMax = value; }
		public double Min { get => min; set => min = value; }
		public double Max { get => max; set => max = value; }

		public PCalculs(string RawCalcule)
		{
			GetInfo(RawCalcule);
		}

		private void GetInfo(string RawCalcule)
		{

			if (RawCalcule.Contains(':'))
			{
				try
				{
					var items = RawCalcule.Split(':');

					if (items.Length >= 2)
					{
						Calcul = items[items.Length - 1];

						foreach (var item in items)
						{
							if (item.ToLower()[0] == 'i' && item.ToLower()[1] == ':' )
							{
								Min = Convert.ToInt32(item.Substring(1));
								IsMin = true;
							}
							if (item.ToLower()[0] == 'a' && item.ToLower()[1] == ':')
							{
								Max = Convert.ToInt32(item.Substring(1));
								IsMax = true;
							}
						}

					}
				}
				catch { }

			}
			else Calcul = RawCalcule;
		}

	}

}
