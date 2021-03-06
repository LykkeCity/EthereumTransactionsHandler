﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Repositories;
using Core.Timers;
using Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Services;
using Services.Models;
using Services.Models.Internal;

namespace Tests
{
	[TestFixture]
	public class QueueListenerServiceTests : BaseTest
	{
		private IncomingSwapRequest _requestSwap = new IncomingSwapRequest
		{
			AmountA = 1,
			AmountB = 2,
			ClientA = "clientA",
			ClientB = "clientB",
			CoinA = "coinA",
			CoinB = "coinB",
			SignA = "signA",
			SignB = "signB",
			TransactionId = new Guid("b40c09bc-3623-4e32-9ede-699bc07774f7")
		};

		private IncomingCashInRequest _requestCashInClientA = new IncomingCashInRequest
		{
			Amount = 1,
			Coin = "coinA",
			To = "clientA"
		};

		private IncomingCashOutRequest _requestCashoutClientB = new IncomingCashOutRequest
		{
			Amount = 1,
			Coin = "coinA",
			To = "clientA",
			Client = "clientB",
			TransactionId = Guid.NewGuid(),
			Sign = "SignB"
		};


		[Test]
		public async Task TestPutToListenerQueue()
		{
			var queueListenerService = Config.Services.GetService<IQueueListenerService>();

			var dbListenerRepo = Config.Services.GetService<IQueueListenerRepository>();
			var coinRepo = Config.Services.GetService<ICoinTransactionRepository>();

			var id = Guid.NewGuid();
			await queueListenerService.PutToListenerQueue(_requestSwap, id);

			var listener = (await dbListenerRepo.GetListeners()).FirstOrDefault();
			Assert.AreEqual(_requestSwap.ClientA, listener.Client);

			var listenerQueue = Config.ListenerQueueFactory(listener.Name);

			var msg = await listenerQueue.PeekRawMessageAsync();
			Assert.NotNull(msg);
			var req = msg.AsString.DeserializeJson<InternalRequest>();
			Assert.AreEqual(RequestType.Swap, req.Action);
			Assert.AreEqual(id, req.Id);
			Assert.AreEqual(0, req.Parents?.Count ?? 0);

			var requestData = req.Request.DeserializeJson<IncomingSwapRequest>();
			Assert.AreEqual(_requestSwap.TransactionId, requestData.TransactionId);

			var coinTransaction = await coinRepo.GetCoinTransaction(req.Id);
			Assert.NotNull(coinTransaction);
			Assert.AreEqual(_requestSwap.ClientA, coinTransaction.ClientA);
			Assert.AreEqual(_requestSwap.ClientB, coinTransaction.ClientB);
			Assert.AreEqual(0, coinTransaction.ConfirmaionLevel);
			Assert.AreEqual(listener.Name, coinTransaction.QueueName);
			Assert.IsFalse(coinTransaction.HasChildClientA || coinTransaction.HasChildClientB);

			await queueListenerService.PutToListenerQueue(_requestSwap, id);
		}

		[Test]
		public async Task TestCoinTransactionsParents()
		{
			var swapId = Guid.NewGuid();
			var cashinId = Guid.NewGuid();
			var cashinId2 = Guid.NewGuid();
			var cashoutId = Guid.NewGuid();

			var dbListenerRepo = Config.Services.GetService<IQueueListenerRepository>();
			var queueListenerService = Config.Services.GetService<IQueueListenerService>();
			var coinRepo = Config.Services.GetService<ICoinTransactionRepository>();

			await queueListenerService.PutToListenerQueue(_requestCashInClientA, cashinId);
			await queueListenerService.PutToListenerQueue(_requestCashInClientA, cashinId2);
			await queueListenerService.PutToListenerQueue(_requestCashoutClientB, cashoutId);
			await queueListenerService.PutToListenerQueue(_requestSwap, swapId);

			var listeners = (await dbListenerRepo.GetListeners()).OrderBy(o => o.Client).ToList();
			Assert.AreEqual(2, listeners.Count());
			Assert.AreEqual(_requestCashInClientA.To, listeners[0].Client);
			Assert.AreEqual(_requestCashoutClientB.Client, listeners[1].Client);

			var listenerQueue = Config.ListenerQueueFactory(listeners[0].Name);
			var requestCashin = (await listenerQueue.GetRawMessageAsync()).AsString.DeserializeJson<InternalRequest>();
			Assert.AreEqual(RequestType.CashIn, requestCashin.Action);
			Assert.IsEmpty(requestCashin.Parents);
			var requestCashin2 = (await listenerQueue.GetRawMessageAsync()).AsString.DeserializeJson<InternalRequest>();

			Assert.Contains(cashinId, requestCashin2.Parents);

			var swapTransaction = await coinRepo.GetCoinTransaction(swapId);

			listenerQueue = Config.ListenerQueueFactory(swapTransaction.QueueName);

			var requestSwap = (await listenerQueue.GetRawMessageAsync()).AsString.DeserializeJson<InternalRequest>();
			if (requestSwap.Action != RequestType.Swap)
				requestSwap = (await listenerQueue.GetRawMessageAsync()).AsString.DeserializeJson<InternalRequest>();

			Assert.Contains(cashinId2, requestSwap.Parents);
			Assert.Contains(cashoutId, requestSwap.Parents);
			Assert.IsFalse(requestSwap.Parents.Contains(cashinId));

			var coinTransaction = await coinRepo.GetCoinTransaction(cashinId);
			Assert.IsTrue(coinTransaction.HasChildClientA);

			coinTransaction = await coinRepo.GetCoinTransaction(cashinId2);
			Assert.IsTrue(coinTransaction.HasChildClientA);

			coinTransaction = await coinRepo.GetCoinTransaction(cashoutId);
			Assert.IsTrue(coinTransaction.HasChildClientA);
		}

		[Test]
		public async Task TestDuplicateExecutionRequest()
		{
			var queueListenerService = Config.Services.GetService<IQueueListenerService>();
			var coinRepo = Config.Services.GetService<ICoinTransactionRepository>();

			var id = Guid.NewGuid();
			await queueListenerService.PutToListenerQueue(_requestSwap, id);
			var listener = await queueListenerService.PutToListenerQueue(_requestSwap, id);
			await listener.Execute();

			await coinRepo.SetTransactionConfirmationLevel(new CoinTransaction { RequestId = id, ConfirmaionLevel = 3, Error = false });

			await listener.Execute();

			Assert.AreEqual(0, await Config.ListenerQueueFactory(listener.Name).Count());
		}

		[Test]
		public async Task TestWaitParentExecution()
		{
			var queueListenerService = Config.Services.GetService<IQueueListenerService>();
			var coinRepo = Config.Services.GetService<ICoinTransactionRepository>();

			var id = Guid.NewGuid();
			var swapId = Guid.NewGuid();
			await queueListenerService.PutToListenerQueue(_requestCashInClientA, id);
			var listener = await queueListenerService.PutToListenerQueue(_requestSwap, swapId);
			await listener.Execute();

			// ReSharper disable once PossibleNullReferenceException
			(listener as TimerPeriod).Working = true;

			var taskSwap = listener.Execute();

			await Task.Delay(1000);
			Assert.IsFalse(taskSwap.IsCompleted);
			await coinRepo.SetTransactionConfirmationLevel(new CoinTransaction { RequestId = id, ConfirmaionLevel = 3, Error = false });

			await Task.Delay(1000);
			Assert.IsTrue(taskSwap.IsCompleted);
			Assert.AreEqual(0, await Config.ListenerQueueFactory(listener.Name).Count());
		}

		[Test]
		public async Task TestParentExecutionFailedAndEmailSended()
		{
			var queueListenerService = Config.Services.GetService<IQueueListenerService>();
			var coinRepo = Config.Services.GetService<ICoinTransactionRepository>();

			var id = Guid.NewGuid();
			var swapId = Guid.NewGuid();
			await queueListenerService.PutToListenerQueue(_requestCashInClientA, id);
			var listener = await queueListenerService.PutToListenerQueue(_requestSwap, swapId);
			await listener.Execute();

			// ReSharper disable once PossibleNullReferenceException
			(listener as TimerPeriod).Working = true;

			var taskSwap = listener.Execute();

			await Task.Delay(1000);
			Assert.IsFalse(taskSwap.IsCompleted);
			await coinRepo.SetTransactionConfirmationLevel(new CoinTransaction { RequestId = id, ConfirmaionLevel = 0, Error = true });

			await Task.Delay(500);

			var queue = Config.Services.GetService<Func<string, IQueueExt>>()(Constants.EmailNotifierQueue);
			Assert.AreEqual(1, await queue.Count());

			await coinRepo.SetTransactionConfirmationLevel(new CoinTransaction { RequestId = id, ConfirmaionLevel = 3, Error = false });

			taskSwap.Wait();
			Assert.IsTrue(taskSwap.IsCompleted);
			Assert.AreEqual(0, await Config.ListenerQueueFactory(listener.Name).Count());
		}

	}
}
