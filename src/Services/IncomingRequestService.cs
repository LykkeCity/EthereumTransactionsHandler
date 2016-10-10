using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Newtonsoft.Json;
using Services.Models;
using Services.Models.Internal;

namespace Services
{
	public interface IIncomingRequestService
	{
		Task<bool> ProcessNextRequest();
	}


	public class IncomingRequestService : IIncomingRequestService
	{
		private readonly IQueueListenerService _queueListenerService;
		private IQueueExt _incomingQueue;

		public IncomingRequestService(Func<string, IQueueExt> queueFactory, IQueueListenerService queueListenerService)
		{
			_queueListenerService = queueListenerService;
			_incomingQueue = queueFactory(Constants.CoinIncomingRequestsQueue);
		}

		public async Task<bool> ProcessNextRequest()
		{
			var msg = await _incomingQueue.GetRawMessageAsync();
			if (msg == null)
				return false;
			var request = JsonConvert.DeserializeObject<IncomingRequest>(msg.AsString);
			var id = new Guid(msg.Id);
			switch (request.Action)
			{
				case RequestType.CashIn:
					var cashin = JsonConvert.DeserializeObject<IncomingCashInRequest>(request.JsonData);
					await _queueListenerService.PutToListenerQueue(cashin, id);
					break;
				case RequestType.CashOut:
					var cashout = JsonConvert.DeserializeObject<IncomingCashOutRequest>(request.JsonData);
					await _queueListenerService.PutToListenerQueue(cashout, id);
					break;
				case RequestType.Swap:
					var swap = JsonConvert.DeserializeObject<IncomingSwapRequest>(request.JsonData);
					await _queueListenerService.PutToListenerQueue(swap, id);
					break;
			}
			await _incomingQueue.FinishRawMessageAsync(msg);
			return true;
		}
	}
}
