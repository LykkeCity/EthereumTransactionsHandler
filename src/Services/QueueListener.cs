using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core.Log;
using Core.Repositories;
using Core.Settings;
using Core.Timers;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Services.Models;
using Services.Models.Internal;

namespace Services
{

	public interface IQueueListener
	{
		void Start();
		Task Stop(bool force);

		Task PutRequestToQueue(InternalRequest request);

		bool IsIdle { get; }
		string Name { get; }
	}

	public class QueueListener : TimerPeriod, IQueueListener
	{
		private const int PeriodSeconds = 2;
		private static readonly TimeSpan WaitTimeout = TimeSpan.FromMinutes(10);

		private DateTime _lastMessage = DateTime.UtcNow;

		private readonly IQueueExt _listenQueue;
		private readonly ICoinTransactionRepository _coinTransactionRepository;
		private readonly IBaseSettings _baseSettings;
		private readonly ILog _logger;
		private readonly IApiCaller _apiCaller;
		private readonly ITransactionRequestMappingRepository _transactionRequestMappingRepository;

		public bool IsIdle => DateTime.UtcNow - _lastMessage > TimeSpan.FromMinutes(10);
		public string Name { get; }

		public QueueListener(string name, IQueueExt listenQueue, ICoinTransactionRepository coinTransactionRepository,
			  IBaseSettings baseSettings, ILog logger, IApiCaller apiCaller,
			  ITransactionRequestMappingRepository transactionRequestMappingRepository)
			: base("QueueListener - " + name, PeriodSeconds * 1000, logger)
		{
			Name = name;
			_listenQueue = listenQueue;
			_coinTransactionRepository = coinTransactionRepository;
			_baseSettings = baseSettings;
			_logger = logger;
			_apiCaller = apiCaller;
			_transactionRequestMappingRepository = transactionRequestMappingRepository;
		}

		public override async Task Execute()
		{
			CloudQueueMessage msg;
			do
			{
				msg = await _listenQueue.PeekRawMessageAsync();
				if (msg != null)
				{
					_lastMessage = DateTime.UtcNow;

					var request = JsonConvert.DeserializeObject<InternalRequest>(msg.AsString);
					await WaitParentExecution(request.Id, request.Parents);
					await ExecuteRequest(request);
					msg = await _listenQueue.GetRawMessageAsync();
					await _listenQueue.FinishRawMessageAsync(msg);
				}
			} while (msg != null && Working);
		}

		private async Task WaitParentExecution(Guid requestId, List<Guid> requestParents)
		{
			if (requestParents == null || requestParents.Count == 0) return;
			DateTime start = DateTime.UtcNow;
			while (requestParents.Count > 0 && Working && DateTime.UtcNow - start < WaitTimeout)
			{
				foreach (var requestParent in requestParents.ToList())
				{
					//TODO: check error flag of parent transaction
					var transaction = await _coinTransactionRepository.GetCoinTransaction(requestParent);
					if (transaction?.ConfirmaionLevel >= _baseSettings.MinTransactionConfirmaionLevel)
						requestParents.Remove(requestParent);
				}
				if (requestParents.Count > 0)
					await Task.Delay(100);
				_lastMessage = DateTime.UtcNow;
			}
			if (requestParents.Count != 0)
				throw new Exception($"Parent transacions didn't execute for request {requestId}");
		}

		private async Task ExecuteRequest(InternalRequest request)
		{
			var transaction = await _coinTransactionRepository.GetCoinTransaction(request.Id);
			if (!string.IsNullOrEmpty(transaction.TransactionHash)) return;
			transaction.TransactionHash = await DoApiCall(request);
			await _transactionRequestMappingRepository.InsertTransactionRequestMapping(new TransactionRequestMapping
			{
				RequestId = request.Id,
				TransactionHash = transaction.TransactionHash
			});
			await _coinTransactionRepository.SetTransactionHash(transaction);
		}

		private async Task<string> DoApiCall(InternalRequest request)
		{
			switch (request.Action)
			{
				case RequestType.CashIn:
					var cashin = JsonConvert.DeserializeObject<IncomingCashInRequest>(request.Request);
					return await _apiCaller.Cashin(cashin.Coin, cashin.To, cashin.Amount);
				case RequestType.CashOut:
					var cashout = JsonConvert.DeserializeObject<IncomingCashOutRequest>(request.Request);
					return await _apiCaller.Cashout(cashout.TransactionId, cashout.Coin, cashout.Client, cashout.To, cashout.Amount, cashout.Sign);
				case RequestType.Swap:
					var swap = JsonConvert.DeserializeObject<IncomingSwapRequest>(request.Request);
					return await _apiCaller.Swap(swap.TransactionId, swap.ClientA, swap.ClientB, swap.CoinA, swap.CoinB, swap.AmountA,
								swap.AmountB, swap.SignA, swap.SignB);
				default:
					throw new ArgumentException("Unexpected request action");
			}
		}

		public Task PutRequestToQueue(InternalRequest request)
		{
			return _listenQueue.PutRawMessageAsync(JsonConvert.SerializeObject(request));
		}

		public async Task Stop(bool force)
		{
			//TODO: exception
			if (!force && await _listenQueue.Count() > 0)
				throw new Exception("Cant stop listener. Queue is not empty.");
			await base.Stop();
			_listenQueue.DeleteIfExists();
		}
	}
}
