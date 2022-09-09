using System;
using System.Net.Mail;
using System.Net;
using System.Windows;
using System.Text;
using System.IO;
using System.Reflection;

namespace PowerNote.Managers
{

	public class CrashReporter
	{
		string m_applicationVersion;
		string m_reportPath;
		private static Mail mail;

		public CrashReporter(string applicationDataPath, string applicationVersion, AppDomain appDomain = null)
		{
			try
			{
				if(File.Exists(Path.Combine(App.HomeConfig.FullPathDir, "CrashReporter.json")))
					mail = Newtonsoft.Json.JsonConvert.DeserializeObject<Mail>(Path.Combine(App.HomeConfig.FullPathDir, "CrashReporter.json"));
				else File.WriteAllText(Path.Combine(App.HomeConfig.FullPathDir, "CrashReporter.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new Mail()));
			}
			catch { }
			

			if (appDomain == null)
			{
				appDomain = AppDomain.CurrentDomain;
			}

			appDomain.UnhandledException += OnUnhandledException;
			//appDomain.FirstChanceException += AppDomain_FirstChanceException;
			m_applicationVersion = applicationVersion;

			m_reportPath = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "Crash Reports");
			Directory.CreateDirectory(m_reportPath);
		}

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			StringBuilder crashReport = null;

			try
			{
				crashReport = new StringBuilder();
				crashReport.AppendLine("Crash Report " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				crashReport.AppendLine("================================");

				Exception exception = e.ExceptionObject as Exception;

				crashReport.AppendLine("Application name: " + (exception != null ? exception.Source : "unknown"));
				crashReport.AppendLine("Application version: " + m_applicationVersion);
				crashReport.AppendLine();

				if (exception != null)
				{
					crashReport.AppendLine(exception.ToString());
				}
				else
				{
					crashReport.AppendLine("Unknown or missing exception object");
				}
			}
			catch
			{
			}

			crashReport.AppendLine();
			crashReport.AppendLine();
			File.AppendAllText(Path.Combine(m_reportPath, "Crash Report " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt"),
				crashReport != null ? crashReport.ToString() : "Error encountered while creating crash report");
			SendErrorTomail(crashReport.ToString());
			if (e.IsTerminating)
			{
				Environment.Exit(-1);
			}
		}

		public static void SendErrorTomail(string txt)
		{
			try
			{
				if (mail == null)
					return;

				SmtpClient mySmtpClient = new SmtpClient(mail.SmtpClient)
				{
					// set smtp-client with basicAuthentication
					UseDefaultCredentials = false,
					Credentials = new
				   System.Net.NetworkCredential(mail.MailAddressMaster, mail.MailAddressMasterPassword),
					Port = 587,
					EnableSsl = true
				};

				var msg = new MailMessage
				{
					From = new MailAddress(mail.MailAddressMaster),
					To = { new MailAddress(mail.MailAddressSleeve) },
					Subject = "PowerNote Crash Report",
					Body = txt
				};
				mySmtpClient.Send(msg);

			}

			catch (SmtpException ex)
			{
				//MessageBox.Show(ex.ToString());
			}
			catch (Exception ex)
			{
				//throw ex;
			}
		}

	}
	public class Mail
	{
		private string mailAddressMaster = "";
		private string mailAddressMasterPassword = "";
		private string mailAddressSleeve = "";
		private string smtpClient = "";

		public string MailAddressMaster { get => mailAddressMaster; set => mailAddressMaster = value; }
		public string MailAddressMasterPassword { get => mailAddressMasterPassword; set => mailAddressMasterPassword = value; }
		public string MailAddressSleeve { get => mailAddressSleeve; set => mailAddressSleeve = value; }
		public string SmtpClient { get => smtpClient; set => smtpClient = value; }

		public Mail()
		{
		}

		public Mail(string mailAddressMaster, string mailAddressMasterPassword, string mailAddressSleeve, string smtpClient)
		{
			MailAddressMaster = mailAddressMaster;
			MailAddressMasterPassword = mailAddressMasterPassword;
			MailAddressSleeve = mailAddressSleeve;
			SmtpClient = smtpClient;
		}
	}
}
