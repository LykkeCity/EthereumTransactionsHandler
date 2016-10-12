using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Core.Timers;
using Services;

namespace TransactionHandlerJob.Jobs
{
	public class ShutdownIdleListenersJob : TimerPeriod
	{
		private readonly IQueueListenerService _queueListenerService;
		private const int PeriodSeconds = 60;

		public ShutdownIdleListenersJob(IQueueListenerService queueListenerService, ILog log) : base("ShutdownIdleListenersJob", PeriodSeconds * 1000, log)
		{
			_queueListenerService = queueListenerService;
		}

		public override Task Execute()
		{
			return	_queueListenerService.ShutdownIdleListeners();
		}
	}
}
