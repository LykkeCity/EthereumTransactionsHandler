using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure.Queue;
using Core;
using Core.Settings;

namespace Services
{
    public interface IQueueFactory
    {
	    IQueueExt GetQueue(string name);
    }

	public class QueueFactory : IQueueFactory
	{
		private readonly IBaseSettings _baseSettings;

		public QueueFactory(IBaseSettings baseSettings)
		{
			_baseSettings = baseSettings;
		}

		public IQueueExt GetQueue(string name)
		{
			return new AzureQueueExt(_baseSettings.Db.DataConnString, Constants.StoragePrefix + name);
		}



	}
}
