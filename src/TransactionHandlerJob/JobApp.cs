using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Services;
using TransactionHandlerJob.Config;
using TransactionHandlerJob.Jobs;

namespace TransactionHandlerJob
{
    public class JobApp
    {
		public IServiceProvider Services { get; set; }

		public void Run(IBaseSettings settings)
	    {
			IServiceCollection collection = new ServiceCollection();
			collection.InitJobDependencies(settings);

			Services = collection.BuildServiceProvider();

		    Services.GetService<IQueueListenerService>().StartupListeners();
			// start monitoring		
			Services.GetService<MonitoringJob>().Start();
		    Services.GetService<ProcessIncomingRequestJob>().Start();
		    Services.GetService<ProcessTransactionEventsJob>().Start();
		    Services.GetService<ShutdownIdleListenersJob>().Start();
			Services.GetService<ProcessClientConfirmationsJob>().Start();
	    }

		public async Task Stop()
		{
			await Services.GetService<ProcessIncomingRequestJob>().Stop();
			await Services.GetService<ProcessTransactionEventsJob>().Stop();
			await Services.GetService<ShutdownIdleListenersJob>().Stop();
			await Services.GetService<ProcessClientConfirmationsJob>().Stop();
			// pause all listeners
			await Services.GetService<IQueueListenerService>().PauseListeners();

			await Services.GetService<MonitoringJob>().Stop();
		}
    }
}
