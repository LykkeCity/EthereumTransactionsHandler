﻿using System;
using AzureRepositories.Azure.Queue;
using AzureRepositories.Azure.Tables;
using AzureRepositories.Log;
using AzureRepositories.Repositories;
using Core;
using Core.Log;
using Core.Repositories;
using Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace AzureRepositories
{
	public static class RegisterReposExt
	{
		public static void RegisterAzureLogs(this IServiceCollection services, IBaseSettings settings, string logPrefix)
		{
			var logToTable = new LogToTable(
				new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, Constants.StoragePrefix + logPrefix + "Error", null),
				new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, Constants.StoragePrefix + logPrefix + "Warning", null),
				new AzureTableStorage<LogEntity>(settings.Db.LogsConnString, Constants.StoragePrefix + logPrefix + "Info", null));

			services.AddSingleton(logToTable);
			services.AddTransient<LogToConsole>();

			services.AddTransient<ILog, LogToTableAndConsole>();
		}

		public static void RegisterAzureStorages(this IServiceCollection services, IBaseSettings settings)
		{
			services.AddSingleton<IMonitoringRepository>(provider => new MonitoringRepository(
				new AzureTableStorage<MonitoringEntity>(settings.Db.SharedConnString, Constants.StoragePrefix + Constants.MonitoringTable,
					provider.GetService<ILog>())));

			services.AddSingleton<ICoinTransactionRepository>((provider => new CoinTransactionRepository(
				new AzureTableStorage<CoinTransationEntity>(settings.Db.DataConnString,
					Constants.StoragePrefix + Constants.CoinTransactionTable,
					provider.GetService<ILog>()))));

			services.AddSingleton<IQueueListenerRepository>((provider => new QueueListenerRepository(
			new AzureTableStorage<DbQueueListenerEntity>(settings.Db.DataConnString,
				Constants.StoragePrefix + Constants.QueueListenerTable,
				provider.GetService<ILog>()))));

			services.AddSingleton<ITransactionRequestMappingRepository>((provider => new TransactionRequestMappingRepository(
			new AzureTableStorage<TransactionRequestMappingEntity>(settings.Db.DataConnString,
				Constants.StoragePrefix + Constants.TransactionRequestMappingTable,
				provider.GetService<ILog>()))));

			services.AddSingleton<ICoinRepository>((provider => new CoinRepository(
			new AzureTableStorage<CoinEntity>(settings.Db.DictsConnString,
				Constants.StoragePrefix + Constants.CoinTable,
				provider.GetService<ILog>()))));

			services.AddSingleton<IConfirmationRequestRepository>((provider => new ConfirmationRequestRepository(
			new AzureTableStorage<ConfirmationRequestEntity>(settings.Db.DataConnString,
				Constants.StoragePrefix + Constants.ConfirmationRequestTable,
				provider.GetService<ILog>()))));
		}

		public static void RegisterAzureQueues(this IServiceCollection services, IBaseSettings settings)
		{
			services.AddTransient<Func<string, string, IQueueExt>>(provider =>
			{
				return ((prefix, name) =>
				{
					if (prefix == Constants.ClientQueuePrefix)
						return new AzureQueueExt(settings.Db.DataConnString, Constants.StoragePrefix + name);

					throw new Exception("Queue is not registered");
				});
			});
			services.AddTransient<Func<string, IQueueExt>>(provider =>
			{
				return (x =>
				{
					switch (x)
					{
						case Constants.CoinTransactionQueue:
							return new AzureQueueExt(settings.Db.EthereumHandlerConnString, Constants.StoragePrefix + x);
						case Constants.CoinIncomingRequestsQueue:
							return new AzureQueueExt(settings.Db.EthereumHandlerConnString, Constants.StoragePrefix + x);
						case Constants.EmailNotifierQueue:
							return new AzureQueueExt(settings.Db.SharedConnString, Constants.StoragePrefix + x);
						case Constants.ConfirmationRequestQueue:
							return new AzureQueueExt(settings.Db.SharedTransactionConnString, Constants.StoragePrefix + x);
						case Constants.ConfirmationResponseQueue:
							return new AzureQueueExt(settings.Db.EthereumHandlerConnString, Constants.StoragePrefix + x);
						default:
							throw new Exception("Queue is not registered");
					}
				});
			});

		}
	}
}
