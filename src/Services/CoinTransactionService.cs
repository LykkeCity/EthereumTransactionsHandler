using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Log;
using Core.Repositories;

namespace Services
{
	public interface ICoinTransactionService
	{
		Task<bool> SetConfirmationLevel(string transactionHash, int level, bool error);
		Task SetTransactionHash(Guid requestId, string transactionHash);
	}


	public class CoinTransactionService : ICoinTransactionService
	{
		private readonly ICoinTransactionRepository _coinTransactionRepository;
		private readonly ITransactionRequestMappingRepository _transactionRequestMappingRepository;
		private readonly ILog _logger;

		public CoinTransactionService(ICoinTransactionRepository coinTransactionRepository,
			ITransactionRequestMappingRepository transactionRequestMappingRepository,
			ILog logger)
		{
			_coinTransactionRepository = coinTransactionRepository;
			_transactionRequestMappingRepository = transactionRequestMappingRepository;
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
	}
}
