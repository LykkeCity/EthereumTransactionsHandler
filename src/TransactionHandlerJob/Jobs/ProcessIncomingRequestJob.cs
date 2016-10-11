using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Core.Timers;
using Services;

namespace TransactionHandlerJob.Jobs
{
	public class ProcessIncomingRequestJob : TimerPeriod
	{
		private readonly IIncomingRequestService _incomingRequestService;
		private const int Period = 2;

		public ProcessIncomingRequestJob(ILog log, IIncomingRequestService incomingRequestService) : base("ProcessIncomingRequestJob", Period * 1000, log)
		{
			_incomingRequestService = incomingRequestService;
		}

		public override async Task Execute()
		{
			while (Working && await _incomingRequestService.ProcessNextRequest())
			{
			}			
		}
	}
}
