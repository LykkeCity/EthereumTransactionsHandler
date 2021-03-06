﻿using AzureRepositories;
using Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Services;
using TransactionHandlerJob.Jobs;

namespace TransactionHandlerJob.Config
{
	public static class RegisterDepencency
	{
		public static void InitJobDependencies(this IServiceCollection collection, IBaseSettings settings)
		{
			collection.AddSingleton(settings);

			collection.RegisterAzureLogs(settings, "TransactionHandler");
			collection.RegisterAzureStorages(settings);
			collection.RegisterAzureQueues(settings);

			collection.RegisterServices();

			RegisterJobs(collection);
		}

		private static void RegisterJobs(IServiceCollection collection)
		{
			collection.AddSingleton<ProcessIncomingRequestJob>();
			collection.AddSingleton<MonitoringJob>();
			collection.AddSingleton<ProcessTransactionEventsJob>();
			collection.AddSingleton<ShutdownIdleListenersJob>();
			collection.AddSingleton<ProcessClientConfirmationsJob>();
		}
	}
}
