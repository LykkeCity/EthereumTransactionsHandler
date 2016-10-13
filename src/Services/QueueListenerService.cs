using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Log;
using Core.Repositories;
using Core.Settings;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Services.Models;
using Services.Models.Internal;
using Core.Utils;

namespace Services
{
	public interface IQueueListenerService
	{
		//Task<IDbQueueListener> GetDbQueueListener(List<string> clients);

		Task StartupListeners();

		IQueueListener CreateAndRunQueueListener(IDbQueueListener listener);
		Task<IQueueListener> PutToListenerQueue(IncomingCashInRequest cashin, Guid id);
		Task<IQueueListener> PutToListenerQueue(IncomingCashOutRequest cashout, Guid id);
		Task<IQueueListener> PutToListenerQueue(IncomingSwapRequest swap, Guid id);
		Task ShutdownIdleListeners(bool force = false);
		Task PauseListeners();
	}

	public class QueueListenerService : IQueueListenerService
	{
		private static readonly SemaphoreSlim _sync = new SemaphoreSlim(1);

		private readonly IQueueListenerRepository _queueListenerRepository;
		private readonly Func<string, IQueueListener> _queueListenerFactory;
		private readonly ICoinTransactionRepository _coinTransactionRepository;
		private readonly IBaseSettings _settings;
		private readonly ILog _logger;

		private readonly Dictionary<string, IQueueListener> _runningListeners = new Dictionary<string, IQueueListener>();

		public QueueListenerService(IQueueListenerRepository queueListenerRepository, Func<string, IQueueListener> queueListenerFactory,
			ICoinTransactionRepository coinTransactionRepository, IBaseSettings settings, ILog logger)
		{
			_queueListenerRepository = queueListenerRepository;
			_queueListenerFactory = queueListenerFactory;
			_coinTransactionRepository = coinTransactionRepository;
			_settings = settings;
			_logger = logger;
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
				await _logger.WriteInfo("QueueListenerService", "GetDbQueueListener", "", $"Created new queue listener {dbQueueListener.Name} for client {dbQueueListener.Client}");
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
					CreateAndRunQueueListener(dbQueueListener);
			}
			finally
			{
				_sync.Release();
			}
		}

		public IQueueListener CreateAndRunQueueListener(IDbQueueListener dbListener)
		{
			var listener = CreateQueueListener(dbListener);
			listener.Start();
			return listener;
		}

		public Task<IQueueListener> PutToListenerQueue(IncomingCashInRequest cashin, Guid id)
		{
			var clients = new List<string> { cashin.To };
			return PutToListenerQueue(RequestType.CashIn, cashin, clients, id);
		}

		public Task<IQueueListener> PutToListenerQueue(IncomingCashOutRequest cashout, Guid id)
		{
			var clients = new List<string> { cashout.Client };
			return PutToListenerQueue(RequestType.CashOut, cashout, clients, id);
		}

		public Task<IQueueListener> PutToListenerQueue(IncomingSwapRequest swap, Guid id)
		{
			var clients = new List<string> { swap.ClientA, swap.ClientB };
			return PutToListenerQueue(RequestType.Swap, swap, clients, id);
		}

		public async Task ShutdownIdleListeners(bool force = false)
		{
			await _sync.WaitAsync();
			try
			{
				foreach (var runningListener in _runningListeners.Select(o => o.Value).ToList())
				{
					if (runningListener.IsIdle || force)
					{
						try
						{
							await runningListener.Stop(force);
							_runningListeners.Remove(runningListener.Name);
							_queueListenerRepository.RemoveListener(runningListener.Name);
						}
						catch (Exception e)
						{
							await _logger.WriteError("QeueuListenerService", "ShutdownIdleListeners", $"Queue name = {runningListener.Name}", e);
						}
					}
				}
			}
			finally
			{
				_sync.Release();
			}
		}

		public async Task PauseListeners()
		{
			await _sync.WaitAsync();
			try
			{
				foreach (var runningListener in _runningListeners.Select(o => o.Value).ToList())
				{
					try
					{
						await runningListener.Pause();
					}
					catch (Exception e)
					{
						await
							_logger.WriteError("QeueuListenerService", "PauseListeners", $"Queue name = {runningListener.Name}", e);
					}
				}
			}
			finally
			{
				_sync.Release();
			}
		}

		private async Task<IQueueListener> PutToListenerQueue(RequestType action, object data, List<string> clients, Guid id)
		{
			clients.Sort();
			var transactions = (await _coinTransactionRepository.GetCoinTransactions(clients, _settings.MinTransactionConfirmaionLevel)).ToList();
			var request = new InternalRequest
			{
				Id = id,
				Action = action,
				Parents = transactions.GroupBy(o => o.QueueName).Select(o => o.OrderByDescending(x => x.CreateDt).First().RequestId).ToList(),
				Request = data.ToJson()
			};
			await _sync.WaitAsync();
			IQueueListener listener = null;
			try
			{
				listener = CreateQueueListener(await GetDbQueueListener(clients));
				await listener.PutRequestToQueue(request);
				await _coinTransactionRepository.AddCoinTransaction(new CoinTransaction
				{
					RequestId = request.Id,
					QueueName = listener.Name,
					ClientA = clients[0],
					ClientB = clients.Count > 1 ? clients[1] : null,
					CreateDt = DateTime.UtcNow,
					RequestData = request.Request
				});
			}
			catch (StorageException ex)
			{
				//if it's not insert duplicate expection then throw
				if (ex.RequestInformation.HttpStatusCode != 409)
					throw;
			}
			finally
			{
				_sync.Release();
			}
			await _coinTransactionRepository.SetChildFlags(transactions, clients);
			return listener;
		}

		private IQueueListener CreateQueueListener(IDbQueueListener listener)
		{
			if (_runningListeners.ContainsKey(listener.Name))
				return _runningListeners[listener.Name];
			var queueListener = _queueListenerFactory(listener.Name);
			_runningListeners.Add(listener.Name, queueListener);
			return queueListener;
		}
	}
}
