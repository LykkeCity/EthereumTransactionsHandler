using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Repositories;
using Core.Settings;
using Newtonsoft.Json;
using Services.Models;
using Services.Models.Internal;

namespace Services
{
	public interface IQueueListenerService
	{
		//Task<IDbQueueListener> GetDbQueueListener(List<string> clients);

		Task StartupListeners();

		IQueueListener RunQueueListener(IDbQueueListener listener);
		Task PutToListenerQueue(IncomingCashInRequest cashin, Guid id);
		Task PutToListenerQueue(IncomingCashOutRequest cashout, Guid id);
		Task PutToListenerQueue(IncomingSwapRequest swap, Guid id);
		Task ShutdownIdleListeners();
	}

	public class QueueListenerService : IQueueListenerService
	{
		private static readonly SemaphoreSlim _sync = new SemaphoreSlim(1);

		private readonly IQueueListenerRepository _queueListenerRepository;
		private readonly Func<string, IQueueListener> _queueListenerFactory;
		private readonly ICoinTransactionRepository _coinTransactionRepository;
		private readonly IBaseSettings _settings;

		private readonly Dictionary<string, IQueueListener> _runningListeners = new Dictionary<string, IQueueListener>();

		public QueueListenerService(IQueueListenerRepository queueListenerRepository, Func<string, IQueueListener> queueListenerFactory,
			ICoinTransactionRepository coinTransactionRepository, IBaseSettings settings)
		{
			_queueListenerRepository = queueListenerRepository;
			_queueListenerFactory = queueListenerFactory;
			_coinTransactionRepository = coinTransactionRepository;
			_settings = settings;
		}

		private async Task<IDbQueueListener> GetDbQueueListener(List<string> clients)
		{
			var dbQueueListener = await _queueListenerRepository.GetQueueListener(clients);
			if (dbQueueListener == null)
			{
				dbQueueListener = new DbQueueListener
				{
					Name = "listener-" + Guid.NewGuid().ToString("n"),
					Client = clients[0],
				};
				await _queueListenerRepository.Insert(dbQueueListener);
			}
			return dbQueueListener;
		}

		public async Task StartupListeners()
		{
			await _sync.WaitAsync();
			try
			{
				var listeners = await _queueListenerRepository.GetListeners();
				foreach (var dbQueueListener in listeners)
					RunQueueListener(dbQueueListener);
			}
			finally
			{
				_sync.Release();
			}
		}

		public IQueueListener RunQueueListener(IDbQueueListener listener)
		{
			if (_runningListeners.ContainsKey(listener.Name))
				return _runningListeners[listener.Name];
			var queueListener = _queueListenerFactory(listener.Name);
			queueListener.Start();
			_runningListeners.Add(listener.Name, queueListener);
			return queueListener;
		}

		public Task PutToListenerQueue(IncomingCashInRequest cashin, Guid id)
		{
			var clients = new List<string> { cashin.To };
			return PutToListenerQueue(RequestType.CashIn, cashin, clients, id);
		}

		public Task PutToListenerQueue(IncomingCashOutRequest cashout, Guid id)
		{
			var clients = new List<string> { cashout.Client };
			return PutToListenerQueue(RequestType.CashIn, cashout, clients, id);
		}

		public Task PutToListenerQueue(IncomingSwapRequest swap, Guid id)
		{
			var clients = new List<string> { swap.ClientA, swap.ClientB };
			return PutToListenerQueue(RequestType.CashIn, swap, clients, id);
		}

		public async Task ShutdownIdleListeners()
		{
			await _sync.WaitAsync();
			try
			{
				foreach (var runningListener in _runningListeners.Select(o => o.Value))
				{
					if (runningListener.IsIdle)
					{
						runningListener.Stop();
						_runningListeners.Remove(runningListener.Name);
						_queueListenerRepository.RemoveListener(runningListener.Name);
					}
				}
			}
			finally
			{
				_sync.Release();
			}
		}

		private async Task PutToListenerQueue(RequestType action, object data, List<string> clients, Guid id)
		{
			clients.Sort();
			var transactions = (await _coinTransactionRepository.GetCoinTransactions(clients, _settings.MinTransactionConfirmaionLevel)).ToList();
			var request = new InternalRequest
			{
				Id = id,
				Action = action,
				Parents = transactions.GroupBy(o => o.QueueName).Select(o => o.OrderByDescending(x => x.CreateDt).First().RequestId).ToList(),
				Request = JsonConvert.SerializeObject(data)
			};
			await _sync.WaitAsync();
			try
			{
				var listener = RunQueueListener(await GetDbQueueListener(clients));
				await listener.PutRequestToQueue(request);
				await _coinTransactionRepository.AddCoinTransaction(new CoinTransaction
				{
					RequestId = request.Id,
					QueueName = listener.Name,
					ClientA = clients[0],
					ClientB = clients.Count > 1 ? clients[1] : null,
					CreateDt = DateTime.UtcNow
				});
			}
			catch (Exception ex)
			{
				//TODO: detect insert duplicate exception
			}
			finally
			{
				_sync.Release();
			}
			await _coinTransactionRepository.SetChildFlags(transactions, clients);
		}
	}
}
