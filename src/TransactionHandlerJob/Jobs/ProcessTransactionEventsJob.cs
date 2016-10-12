using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Core.Timers;
using Services;

namespace TransactionHandlerJob.Jobs
{
	public class ProcessTransactionEventsJob : TimerPeriod
	{
		private readonly ITransactionUpdateService _transactionUpdateService;
		public const int PeriodSeconds = 2;

		public ProcessTransactionEventsJob(ITransactionUpdateService transactionUpdateService, ILog log) : base("ProcessTransactionEventsJob", PeriodSeconds * 1000, log)
		{
			_transactionUpdateService = transactionUpdateService;
		}

		public override async Task Execute()
		{
			while (Working && await _transactionUpdateService.GetAndProcessTransactionStatus())
			{

			}
		}
	}
}
