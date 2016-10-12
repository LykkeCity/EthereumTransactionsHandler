using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Repositories;
using Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using Services;
using Services.Models;
using Services.Models.Internal;

namespace Tests
{
	[TestFixture]
	public class IncomingRequestTests : BaseTest
	{

		private readonly IncomingRequest _requestSwap = new IncomingRequest
		{
			JsonData = new IncomingSwapRequest
			{
				AmountA = 1,
				AmountB = 2,
				ClientA = "clientB",
				ClientB = "clientA",
				CoinA = "coinA",
				CoinB = "coinB",
				SignA = "signA",
				SignB = "signB",
				TransactionId = new Guid("b40c09bc-3623-4e32-9ede-699bc07774f7")
			}.ToJson(),
			Action = RequestType.Swap
		};

		private readonly IncomingRequest _requestCashInClientA = new IncomingRequest
		{
			JsonData = new IncomingCashInRequest
			{
				Amount = 1,
				Coin = "coinA",
				To = "clientA"
			}.ToJson(),
			Action = RequestType.CashIn
		};

		[Test]
		public async Task ComplexTest()
		{
			var incomingRequestService = Config.Services.GetService<IIncomingRequestService>();
			var queueFactory = Config.Services.GetService<Func<string, IQueueExt>>();
			var coinTransactionRepo = Config.Services.GetService<ICoinTransactionRepository>();
			var listenerQueueService = Config.Services.GetService<IQueueListenerService>();
			var transactionUpdateService = Config.Services.GetService<ITransactionUpdateService>();

			var incomingQueue = queueFactory(Constants.CoinIncomingRequestsQueue);

			await incomingQueue.PutRawMessageAsync(_requestSwap.ToJson());
			await incomingQueue.PutRawMessageAsync(_requestCashInClientA.ToJson());


			var messages = (await incomingQueue.PeekMessagesAsync(2)).ToList();


			await incomingRequestService.ProcessNextRequest();
			await incomingRequestService.ProcessNextRequest();
			
			await Task.Delay(500);

			var transactionSwap = await coinTransactionRepo.GetCoinTransaction(new Guid(messages[0].Id));

			var transactionCashin = await coinTransactionRepo.GetCoinTransaction(new Guid(messages[1].Id));
			Assert.IsNull(transactionCashin.TransactionHash);

			var transactionStateQueue = queueFactory(Constants.CoinTransactionQueue);

			await transactionStateQueue.PutRawMessageAsync(new CoinTransactionStatus
			{
				TransactionHash = transactionSwap.TransactionHash,
				ConfirmationLevel = 1
			}.ToJson());

			await transactionUpdateService.GetAndProcessTransactionStatus();
			await Task.Delay(200);
			transactionCashin = await coinTransactionRepo.GetCoinTransaction(new Guid(messages[1].Id));
			Assert.IsNotNull(transactionCashin.TransactionHash);

			await transactionStateQueue.PutRawMessageAsync(new CoinTransactionStatus
			{
				TransactionHash = transactionCashin.TransactionHash,
				ConfirmationLevel = 2
			}.ToJson());
			await transactionUpdateService.GetAndProcessTransactionStatus();

			transactionCashin = await coinTransactionRepo.GetCoinTransaction(new Guid(messages[1].Id));
			Assert.AreEqual(2, transactionCashin.ConfirmaionLevel);

			await listenerQueueService.ShutdownIdleListeners(true);
		}
	}
}
