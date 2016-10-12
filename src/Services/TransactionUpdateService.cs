using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Log;
using Core.Repositories;
using Core.Timers;
using Core.Timers.Interfaces;
using Core.Utils;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Services.Models.Internal;

namespace Services
{
	public interface ITransactionUpdateService
	{
		Task<bool> GetAndProcessTransactionStatus();
	}

	public class TransactionUpdateService : ITransactionUpdateService
	{
		private readonly IQueueExt _queue;
		private readonly ILog _logger;
		private readonly ICoinTransactionService _coinTransactionService;

		public TransactionUpdateService(ILog logger, Func<string, IQueueExt> queueFactory,
			ICoinTransactionService coinTransactionService)
		{
			_queue = queueFactory(Constants.CoinTransactionQueue);
			_logger = logger;
			_coinTransactionService = coinTransactionService;
		}

		public async Task<bool> GetAndProcessTransactionStatus()
		{
			var msg = await _queue.PeekRawMessageAsync();

			if (msg == null)
				return false;

			var status = msg.AsString.DeserializeJson<CoinTransactionStatus>();
			await _logger.WriteInfo("TransactionUpdateService", "GetAndProcessTransactionStatus", "", $"Received new transaction state event \"{msg.AsString}\"");
			if (!await _coinTransactionService.SetConfirmationLevel(status.TransactionHash, status.ConfirmationLevel, status.Error))
			{
				await _queue.PutRawMessageAsync(status.ToJson());
				await _logger.WriteInfo("TransactionUpdateService", "GetAndProcessTransactionStatus", "",
						$"Requeue transaction state event \"{msg.AsString}\"");
			}
			msg = await _queue.GetRawMessageAsync();
			await _queue.FinishRawMessageAsync(msg);
			return true;
		}
	}
}
