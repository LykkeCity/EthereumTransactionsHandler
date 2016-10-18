using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Utils;
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
			var msg = await _incomingQueue.PeekRawMessageAsync();
			if (msg == null)
				return false;
			var request = msg.AsString.DeserializeJson<IncomingRequest>();
			var id = new Guid(msg.Id);
			switch (request.Action)
			{
				case RequestType.CashIn:
					var cashin = request.JsonData.DeserializeJson<IncomingCashInRequest>();
					(await _queueListenerService.PutToListenerQueue(cashin, id))?.Start();
					break;
				case RequestType.CashOut:
					var cashout = request.JsonData.DeserializeJson<IncomingCashOutRequest>();
					(await _queueListenerService.PutToListenerQueue(cashout, id)).Start();
					break;
				case RequestType.Swap:
					var swap = request.JsonData.DeserializeJson<IncomingSwapRequest>();
					(await _queueListenerService.PutToListenerQueue(swap, id)).Start();
					break;
				case RequestType.Transfer:
					var transfer = request.JsonData.DeserializeJson<IncomingTransferRequest>();
					(await _queueListenerService.PutToListenerQueue(transfer, id)).Start();
					break;
			}
			msg = await _incomingQueue.GetRawMessageAsync();
			await _incomingQueue.FinishRawMessageAsync(msg);
			return true;
		}
	}
}
