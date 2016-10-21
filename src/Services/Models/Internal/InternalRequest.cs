using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Repositories;
using Core.Utils;

namespace Services.Models.Internal
{
	public class InternalRequest
	{
		public Guid Id { get; set; }

		public RequestType Action { get; set; }

		public List<string> Clients { get; set; }
		
		public string Request { get; set; }

		/// <summary>
		/// List of requests that should be executed before this request
		/// </summary>
		public List<Guid> Parents { get; set; }

		public Task<string> BuildHash(ICoinRepository coinRepository)
		{
			switch (Action)
			{
				case RequestType.CashIn:
					throw new Exception("CashIn is working without signature");
				case RequestType.CashOut:
					return Request.DeserializeJson<IncomingCashOutRequest>().BuildHash(coinRepository);					
				case RequestType.Swap:
					return Request.DeserializeJson<IncomingSwapRequest>().BuildHash(coinRepository);
				case RequestType.Transfer:
					return Request.DeserializeJson<IncomingTransferRequest>().BuildHash(coinRepository);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	public enum RequestType
	{
		CashIn,
		CashOut,
		Swap,
		Transfer
	}
}
