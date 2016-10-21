using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Core.Settings;
using TransactionHandlerJob;

namespace TransactionHandlerRunner
{
    public class Program
    {
	    static JobApp JobApp { get; set; }

	    public static void Main(string[] args)
		{
			Console.Clear();
			Console.Title = "Ethereum Transaction Handler - Ver. " + Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion;

			var settings = GetSettings();
			if (settings == null)
			{
				Console.WriteLine("Press any key to exit...");
				Console.ReadKey();
				return;
			}

			try
			{
				CheckSettings(settings);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine("Press any key to exit...");
				Console.ReadKey();
				return;
			}

			Console.WriteLine("Settings checked!");

			try
			{
				JobApp = new JobApp();
				JobApp.Run(settings);
			}
			catch (Exception e)
			{
				Console.WriteLine("cannot start jobs! Exception: " + e.Message);
				Console.WriteLine("Press any key to exit...");
				Console.ReadKey();
				return;
			}

			Console.WriteLine("Web job started");
			Console.WriteLine("Utc time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

			Console.WriteLine("Press 'q' to quit.");

			SetConsoleCtrlHandler(ConsoleCtrlCheck, true);

			while (Console.ReadLine() != "q") continue;

			JobApp.Stop().Wait();
		}

		static BaseSettings GetSettings()
		{
			var settingsData = ReadSettingsFile();

			if (string.IsNullOrWhiteSpace(settingsData))
			{
				Console.WriteLine("Please, provide generalsettings.json file");
				return null;
			}

			BaseSettings settings = GeneralSettingsReader.ReadSettingsFromData<BaseSettings>(settingsData);

			return settings;
		}

		static string ReadSettingsFile()
		{
			try
			{
#if DEBUG
				return File.ReadAllText(@"..\..\settings\generalsettings.json");
#else
				return File.ReadAllText("generalsettings.json");
#endif
			}
			catch (Exception e)
			{
				return null;
			}
		}

		static void CheckSettings(BaseSettings settings)
		{
			if (string.IsNullOrWhiteSpace(settings.ApiUrl))
				throw new Exception("ApiUrl is missing");

			if (string.IsNullOrWhiteSpace(settings.Db?.DataConnString))
				throw new Exception("DataConnString is missing");
			if (string.IsNullOrWhiteSpace(settings.Db?.LogsConnString))
				throw new Exception("LogsConnString is missing");
			if (string.IsNullOrWhiteSpace(settings.Db?.ExchangeQueueConnString))
				throw new Exception("ExchangeQueueConnString is missing");
			if (string.IsNullOrWhiteSpace(settings.Db?.EthereumNotificationsConnString))
				throw new Exception("EthereumNotificationsConnString is missing");
		}

		[DllImport("kernel32")]
		public static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);

		public delegate bool HandlerRoutine(CtrlTypes ctrlType);

		public enum CtrlTypes
		{
			CTRL_C_EVENT = 0,
			CTRL_BREAK_EVENT,
			CTRL_CLOSE_EVENT,
			CTRL_LOGOFF_EVENT = 5,
			CTRL_SHUTDOWN_EVENT
		}

		private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
		{
			JobApp.Stop().Wait();
			return true;
		}
	}
}
