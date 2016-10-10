using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Models.Internal
{
	public class InternalRequest
	{
		public Guid Id { get; set; }

		public RequestType Action { get; set; }

		public string Request { get; set; }

		/// <summary>
		/// List of requests that should be executed before this request
		/// </summary>
		public List<Guid> Parents { get; set; }
	}

	public enum RequestType
	{
		CashIn = 0,
		CashOut = 1,
		Swap = 3
	}
}
