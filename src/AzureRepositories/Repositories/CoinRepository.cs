using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure;
using Core;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Repositories
{
	public class CoinEntity : TableEntity, ICoin
	{
		public const string PartitionKey = Constants.EthereumBlockchain;

		public string Blockchain => PartitionKey;
		public string Name => RowKey;
		public string Address { get; set; }
		public string Multiplier { get; set; }
	}


	public class CoinRepository : ICoinRepository
	{
		private readonly INoSQLTableStorage<CoinEntity> _table;

		public CoinRepository(INoSQLTableStorage<CoinEntity> table)
		{
			_table = table;
		}

		public async Task<ICoin> GetCoin(string address)
		{
			var coin = (await _table.GetDataAsync(CoinEntity.PartitionKey, o => o.Address == address)).FirstOrDefault();
			if (coin == null)
				throw new Exception("Unknown coin address - " + address);
			return coin;
		}
	}
}
