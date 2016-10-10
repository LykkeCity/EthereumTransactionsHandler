using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

			services.AddSingleton<IQueueFactory, QueueFactory>();
			services.AddTransient<Func<string, IQueueListener>>(provider =>
			{
				return x => new QueueListener(x, provider.GetService<IQueueFactory>().GetQueue(x),
					provider.GetService<ICoinTransactionRepository>(),
					provider.GetService<IBaseSettings>(),
					provider.GetService<ILog>(),
					provider.GetService<IApiCaller>());
			});
			services.AddSingleton<IQueueListenerService, IQueueListenerService>();
			services.AddTransient<IRestClient, RestClient>();
			services.AddTransient<IApiCaller, ApiCaller>();
		}
	}
}
