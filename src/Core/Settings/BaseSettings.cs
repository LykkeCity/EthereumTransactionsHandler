using System;
using System.Collections.Generic;
using System.Numerics;

namespace Core.Settings
{
	public interface IBaseSettings
	{
		DbSettings Db { get; set; }
		
		int MinTransactionConfirmaionLevel { get; set; }
		string ApiUrl { get; set; }
	}

	public class BaseSettings : IBaseSettings
	{
		public DbSettings Db { get; set; }

		public int MinTransactionConfirmaionLevel { get; set; } = 1;
		public string ApiUrl { get; set; }
	}

	public class DbSettings
	{
		public string DataConnString { get; set; }
		public string LogsConnString { get; set; }

        public string DictsConnString { get; set; }

	    public string EthereumHandlerConnString { get; set; }

        public string SharedConnString { get; set; }
        public string SharedTransactionConnString { get; set; }
    }
}
