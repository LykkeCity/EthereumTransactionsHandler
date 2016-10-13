using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core.Log;
using Core.Repositories;
using Core.Settings;
using Core.Timers;
using Core.Utils;
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
		Task Execute();

		Task PutRequestToQueue(InternalRequest request);

		bool IsIdle { get; }
		string Name { get; }
		Task Pause();
	}

	public class QueueListener : TimerPeriod, IQueueListener
	{
		private const int PeriodSeconds = 2;
		private static readonly TimeSpan WaitTimeout = TimeSpan.FromMinutes(10);
		private const int ErrorNotifyPeriodMinutes = 10;

		private DateTime _lastMessage = DateTime.UtcNow;

		private readonly IQueueExt _listenQueue;
		private readonly ICoinTransactionRepository _coinTransactionRepository;
		private readonly IBaseSettings _baseSettings;
		private readonly ILog _logger;
		private readonly IApiCaller _apiCaller;
		private readonly ICoinTransactionService _coinTransactionService;
		private readonly IEmailNotifierService _emailNotifier;

		public bool IsIdle => DateTime.UtcNow - _lastMessage > TimeSpan.FromMinutes(10);
		public string Name { get; }

		public QueueListener(string name, IQueueExt listenQueue, ICoinTransactionRepository coinTransactionRepository,
			  IBaseSettings baseSettings, ILog logger, IApiCaller apiCaller,
			  ICoinTransactionService coinTransactionService, IEmailNotifierService emailNotifier)
			: base("QueueListener - " + name, PeriodSeconds * 1000, logger)
		{
			Name = name;
			_listenQueue = listenQueue;
			_coinTransactionRepository = coinTransactionRepository;
			_baseSettings = baseSettings;
			_logger = logger;
			_apiCaller = apiCaller;
			_coinTransactionService = coinTransactionService;
			_emailNotifier = emailNotifier;
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

					var request = msg.AsString.DeserializeJson<InternalRequest>();
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
			int retries = 0;
			while (requestParents.Count > 0 && DateTime.UtcNow - start < WaitTimeout)
			{
				foreach (var requestParent in requestParents.ToList())
				{
					var transaction = await _coinTransactionRepository.GetCoinTransaction(requestParent);

					if (transaction.Error)
					{
						if (retries % (ErrorNotifyPeriodMinutes * 60 * 10) == 0)
							_emailNotifier.Warning("Ethereum transaction error", $"Transaction is failed! Request id: [{transaction.RequestId}]");

						retries++;
					}

					if (transaction?.ConfirmaionLevel >= _baseSettings.MinTransactionConfirmaionLevel)
						requestParents.Remove(requestParent);


				}
				if (requestParents.Count > 0)
					await Task.Delay(100);
				_lastMessage = DateTime.UtcNow;
				if (!Working) break;
			}
			if (requestParents.Count != 0)
				throw new Exception($"Parent transactions didn't execute for request {requestId}");
		}

		private async Task ExecuteRequest(InternalRequest request)
		{
			var transaction = await _coinTransactionRepository.GetCoinTransaction(request.Id);
			if (!string.IsNullOrEmpty(transaction.TransactionHash)) return;

			await _logger.WriteInfo("QueueListener", "ExecuteRequest", "", $"Start execute request [{request.Id}]");

			transaction.TransactionHash = await DoApiCall(request);
			await _coinTransactionService.SetTransactionHash(request.Id, transaction.TransactionHash);

			await _logger.WriteInfo("QueueListener", "ExecuteRequest", "", $"Request executed [{request.Id}]");
		}

		private async Task<string> DoApiCall(InternalRequest request)
		{
			switch (request.Action)
			{
				case RequestType.CashIn:
					var cashin = request.Request.DeserializeJson<IncomingCashInRequest>();
					return await _apiCaller.Cashin(request.Id, cashin.TransactionId, cashin.Coin, cashin.To, cashin.Amount);
				case RequestType.CashOut:
					var cashout = request.Request.DeserializeJson<IncomingCashOutRequest>();
					return await _apiCaller.Cashout(request.Id, cashout.TransactionId, cashout.Coin, cashout.Client, cashout.To, cashout.Amount, cashout.Sign);
				case RequestType.Swap:
					var swap = request.Request.DeserializeJson<IncomingSwapRequest>();
					return await _apiCaller.Swap(request.Id, swap.TransactionId, swap.ClientA, swap.ClientB, swap.CoinA, swap.CoinB, swap.AmountA,
								swap.AmountB, swap.SignA, swap.SignB);
				default:
					throw new ArgumentException("Unexpected request action");
			}
		}

		public Task PutRequestToQueue(InternalRequest request)
		{
			return _listenQueue.PutRawMessageAsync(request.ToJson());
		}

		public async Task Stop(bool force)
		{
			//TODO: exception
			if (!force && await _listenQueue.Count() > 0)
				throw new Exception("Cant stop listener. Queue is not empty.");
			await base.Stop();
			_listenQueue.DeleteIfExists();
		}

		public async Task Pause()
		{
			await base.Stop();
		}
	}
}
