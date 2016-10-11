using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Log;
using Core.Settings;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Services;
using TransactionHandlerJob.Config;

namespace Tests
{
	[SetUpFixture]
	public class Config
	{
		public static IServiceProvider Services { get; set; }
		public static ILog Logger => Services.GetService<ILog>();

		public static Func<string, IQueueExt> ListenerQueueFactory
			=> (name) => Services.GetService<Func<string, string, IQueueExt>>()(Constants.ClientQueuePrefix, name);

		private IBaseSettings ReadSettings()
		{
			try
			{
				var json = File.ReadAllText(@"..\settings\generalsettings.json");
				if (string.IsNullOrWhiteSpace(json))
				{

					return null;
				}
				BaseSettings settings = GeneralSettingsReader.ReadSettingsFromData<BaseSettings>(json);

				return settings;
			}
			catch (Exception e)
			{
				return null;
			}
		}


		[OneTimeSetUp]
		public void Initialize()
		{
			Constants.StoragePrefix = "tests";

			IServiceCollection collection = new ServiceCollection();
			var settings = ReadSettings();

			Assert.NotNull(settings, "Please, provide generalsettings.json file");

			collection.InitJobDependencies(settings);
			collection.AddTransient<IApiCaller, ApiCallerMock>();

			Services = collection.BuildServiceProvider();						
		}
	}
}
