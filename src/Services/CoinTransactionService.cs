using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Log;
using Core.Repositories;
using Core.Utils;

namespace Services
{
	public interface ICoinTransactionService
	{
		Task<bool> SetConfirmationLevel(string transactionHash, int level, bool error);
		Task SetTransactionHash(Guid requestId, string transactionHash);
		Task RequestClientConfirmation(Guid requestId, string client, string hash);
		Task<bool> ProcessClientConfirmation();
	}


	public class CoinTransactionService : ICoinTransactionService
	{
		private readonly ICoinTransactionRepository _coinTransactionRepository;
		private readonly ITransactionRequestMappingRepository _transactionRequestMappingRepository;
		private readonly IConfirmationRequestRepository _confirmationRequestRepository;
		private readonly ILog _logger;
		private readonly IQueueExt _confirmationRequestQueue;
		private readonly IQueueExt _confirmationResponseQueue;

		public CoinTransactionService(ICoinTransactionRepository coinTransactionRepository,
			ITransactionRequestMappingRepository transactionRequestMappingRepository, IConfirmationRequestRepository confirmationRequestRepository,
			Func<string, IQueueExt> queueFactory, ILog logger)
		{
			_coinTransactionRepository = coinTransactionRepository;
			_transactionRequestMappingRepository = transactionRequestMappingRepository;
			_confirmationRequestRepository = confirmationRequestRepository;

			_confirmationRequestQueue = queueFactory(Constants.ConfirmationRequestQueue);
			_confirmationResponseQueue = queueFactory(Constants.ConfirmationResponseQueue);
			_logger = logger;
		}

		public async Task<bool> SetConfirmationLevel(string transactionHash, int level, bool error)
		{
			var mapping = await _transactionRequestMappingRepository.GetTransactionRequestMapping(transactionHash);
			if (mapping == null)
			{
				await _logger.WriteWarning("CoinTransactionService", "SetConfirmationLevel", "",
						$"Not found request by transactionHash hash {transactionHash}");
				return false;
			}
			await _coinTransactionRepository.SetTransactionConfirmationLevel(new CoinTransaction
			{
				ConfirmaionLevel = level,
				Error = error,
				RequestId = mapping.RequestId
			});
			return true;
		}

		public async Task SetTransactionHash(Guid requestId, string transactionHash)
		{
			await _transactionRequestMappingRepository.InsertTransactionRequestMapping(new TransactionRequestMapping
			{
				RequestId = requestId,
				TransactionHash = transactionHash
			});
			await _coinTransactionRepository.SetTransactionHash(new CoinTransaction
			{
				RequestId = requestId,
				TransactionHash = transactionHash
			});
		}

		public async Task RequestClientConfirmation(Guid requestId, string client, string hash)
		{
			var confirmation = await _confirmationRequestRepository.GetConfirmationRequest(requestId, client);
			if (confirmation != null) return;
			await _confirmationRequestQueue.PutRawMessageAsync(new { requestId = requestId, client = client, hash = hash }.ToJson());
			await _confirmationRequestRepository.InsertConfirmationRequest(new ConfirmationRequest
			{
				Client = client,
				RequestId = requestId
			});
		}

		public async Task<bool> ProcessClientConfirmation()
		{
			var msg = await _confirmationResponseQueue.GetRawMessageAsync();
			if (msg == null) return false;
			var clientSignature = msg.AsString.DeserializeJson<ClientSignature>();
			await _logger.WriteInfo("CoinTransactionService", "ProcessClientConfirmation", "",
					$"New signature from client RequestId={clientSignature.RequestId}, Client={clientSignature.Client}");
			await _coinTransactionRepository.SetSignature(clientSignature.RequestId, clientSignature.Client, clientSignature.Signature);
			await _confirmationResponseQueue.FinishRawMessageAsync(msg);
			return true;
		}

		// ReSharper disable once ClassNeverInstantiated.Local
		private class ClientSignature
		{
			public string Client { get; set; }
			public Guid RequestId { get; set; }
			public string Signature { get; set; }
		}
	}
}
