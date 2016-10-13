using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Log;
using Core.Repositories;
using Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;

namespace Services
{
	public static class RegisterDependency
	{
		public static void RegisterServices(this IServiceCollection services)
		{
			services.AddTransient<Func<string, IQueueListener>>(provider =>
			{
				return x => new QueueListener(x, provider.GetService<Func<string, string, IQueueExt>>()(Constants.ClientQueuePrefix, x),
					provider.GetService<ICoinTransactionRepository>(),
					provider.GetService<IBaseSettings>(),
					provider.GetService<ILog>(),
					provider.GetService<IApiCaller>(),
					provider.GetService<ICoinTransactionService>(),
					provider.GetService<IEmailNotifierService>());
			});
			services.AddTransient<IIncomingRequestService, IncomingRequestService>();			
			services.AddSingleton<ITransactionUpdateService, TransactionUpdateService>();
			services.AddSingleton<IQueueListenerService, QueueListenerService>();
			services.AddTransient<IRestClient, RestClient>();
			services.AddTransient<IApiCaller, ApiCaller>();
			services.AddTransient<ICoinTransactionService, CoinTransactionService>();
			services.AddTransient<IEmailNotifierService, EmailNotifierService>();
		}
	}
}
