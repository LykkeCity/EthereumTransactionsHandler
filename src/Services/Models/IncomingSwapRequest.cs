using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Models
{
	public class IncomingSwapRequest
	{
		public Guid TransactionId { get; set; }

		public string ClientA { get; set; }

		public string ClientB { get; set; }

		public string CoinA { get; set; }

		public string CoinB { get; set; }

		public decimal AmountA { get; set; }
		public decimal AmountB { get; set; }

		public string SignA { get; set; }
		public string SignB { get; set; }
	}
}
