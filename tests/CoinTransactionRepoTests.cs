using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Repositories;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
namespace Tests
{
	[TestFixture]
	public class CoinTransactionRepoTests : BaseTest
	{
		[Test]
		public async Task TestSetTransactionHash()
		{
			var repo = Config.Services.GetService<ICoinTransactionRepository>();
			var transaction = new CoinTransaction
			{
				RequestId = Guid.NewGuid()
			};
			await repo.AddCoinTransaction(transaction);

			transaction.TransactionHash = "testHash";

			await repo.SetTransactionHash(transaction);

			Assert.AreEqual(transaction.TransactionHash, (await repo.GetCoinTransaction(transaction.RequestId)).TransactionHash);
		}

		[Test]
		public async Task TestSetChildFlags()
		{
			var repo = Config.Services.GetService<ICoinTransactionRepository>();
			var transaction = new CoinTransaction
			{
				RequestId = Guid.NewGuid(),
				ClientA = "client",
				ClientB = "clientB"
			};
			await repo.AddCoinTransaction(transaction);			

			await repo.SetChildFlags(new ICoinTransaction[] {transaction}, new List<string>() {"clientB", "clientA"});
			var tr = await repo.GetCoinTransaction(transaction.RequestId);
			Assert.IsFalse(tr.HasChildClientA);
			Assert.IsTrue(tr.HasChildClientB);
		}

		[Test]
		public async Task TestSetTransactionConfirmation()
		{
			var repo = Config.Services.GetService<ICoinTransactionRepository>();
			var transactionRequestMappingRepo = Config.Services.GetService<ITransactionRequestMappingRepository>();
			var transaction = new CoinTransaction
			{
				TransactionHash = "hash",
				RequestId = Guid.NewGuid()
			};
			await repo.AddCoinTransaction(transaction);
			await transactionRequestMappingRepo.InsertTransactionRequestMapping(new TransactionRequestMapping
			{
				RequestId = transaction.RequestId,
				TransactionHash = transaction.TransactionHash
			});

			var mapping = await transactionRequestMappingRepo.GetTransactionRequestMapping(transaction.TransactionHash);
			transaction.RequestId = mapping.RequestId;
			transaction.ConfirmaionLevel = 2;
			transaction.Error = true;

			await repo.SetTransactionConfirmationLevel(transaction);
			var tr = await repo.GetCoinTransaction(transaction.RequestId);

			Assert.IsTrue(tr.Error);
			Assert.AreEqual(transaction.ConfirmaionLevel, tr.ConfirmaionLevel);
		}
	}
}
