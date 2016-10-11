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
		private readonly ICoinTransactionRepository _coinTransactionRepository;
		private readonly ITransactionRequestMappingRepository _transactionRequestMappingRepository;
		private readonly IQueueExt _queue;
		private readonly ILog _logger;

		public TransactionUpdateService(ILog logger, Func<string, IQueueExt> queueFactory,
			ICoinTransactionRepository coinTransactionRepository, ITransactionRequestMappingRepository transactionRequestMappingRepository)
		{
			_coinTransactionRepository = coinTransactionRepository;
			_transactionRequestMappingRepository = transactionRequestMappingRepository;
			_queue = queueFactory(Constants.CoinTransactionQueue);
			_logger = logger;
		}

		public async Task<bool> GetAndProcessTransactionStatus()
		{
			var msg = await _queue.PeekRawMessageAsync();

			if (msg == null)
				return false;

			var status = JsonConvert.DeserializeObject<CoinTransactionStatus>(msg.AsString);

			var transactionRequestMapping =
				await _transactionRequestMappingRepository.GetTransactionRequestMapping(status.TransactionHash);
			if (transactionRequestMapping == null)
				await _logger.WriteWarning("TransactionUpdateService", "GetAndProcessTransactionStatus", "", $"Not found request by transaction hash {status.TransactionHash}");
			else
				await _coinTransactionRepository.SetTransactionConfirmationLevel(new CoinTransaction
				{
					ConfirmaionLevel = status.ConfirmationLevel,
					Error = status.Error,
					RequestId = transactionRequestMapping.RequestId
				});
			msg = await _queue.GetRawMessageAsync();
			await _queue.FinishRawMessageAsync(msg);

			return true;
		}
	}
}
