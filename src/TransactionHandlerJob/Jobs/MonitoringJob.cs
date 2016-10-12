﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Core.Repositories;
using Core.Timers;

namespace TransactionHandlerJob.Jobs
{
	public class MonitoringJob : TimerPeriod
	{
		private const int TimerPeriodSeconds = 30;

		private readonly IMonitoringRepository _repository;

		public MonitoringJob(IMonitoringRepository repository, ILog logger)
			: this("MonitoringJob", TimerPeriodSeconds * 1000, logger)
		{
			_repository = repository;
		}

		private MonitoringJob(string componentName, int periodMs, ILog log) : base(componentName, periodMs, log)
		{
		}

		public override async Task Execute()
		{
			await _repository.SaveAsync(new Monitoring
			{
				DateTime = DateTime.UtcNow,
				ServiceName = "EthereumTransactionsHandler",
				Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion
			});
		}
	}
}
