using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Core.Timers;
using Services;

namespace TransactionHandlerJob.Jobs
{
	public class ProcessClientConfirmationsJob : TimerPeriod
	{
		private readonly ICoinTransactionService _coinTransactionService;
		public const int PeriodSeconds = 2;
		public ProcessClientConfirmationsJob(ICoinTransactionService coinTransactionService, ILog log) : base("ProcessClientConfirmationsJob", PeriodSeconds * 1000, log)
		{
			_coinTransactionService = coinTransactionService;
		}

		public override async Task Execute()
		{
			while (Working && await _coinTransactionService.ProcessClientConfirmation())
			{

			}
		}
	}
}
