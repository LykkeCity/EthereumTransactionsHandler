using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Repositories
{

	public class DbQueueListenerEntity : TableEntity, IDbQueueListener
	{
		public string Name => RowKey;
		public string Client { get; }
	}


	public class QueueListenerRepository : IQueueListenerRepository
	{
		private readonly INoSQLTableStorage<DbQueueListenerEntity> _table;

		public QueueListenerRepository(INoSQLTableStorage<DbQueueListenerEntity> table)
		{
			_table = table;
		}

		public async Task<IDbQueueListener> GetQueueListener(List<string> clients)
		{
			return (await _table.GetDataAsync(o => clients.Contains(o.Client))).OrderBy(o => o.Name).FirstOrDefault();
		}

		public Task Insert(IDbQueueListener dbQueueListener)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<IDbQueueListener>> GetListeners()
		{
			throw new NotImplementedException();
		}

		public void RemoveListener(string runningListenerName)
		{
			throw new NotImplementedException();
		}
	}
}
