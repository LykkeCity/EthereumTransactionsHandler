using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Services;

namespace Tests
{
	public class ApiCallerMock : IApiCaller
	{
		public Task<string> Cashin(Guid requestId, Guid id, string coin, string to, decimal amount)
		{
			return Task.FromResult(Guid.NewGuid().ToString());
		}

		public Task<string> Cashout(Guid requestId, Guid id, string coin, string client, string to, decimal amount, string sign)
		{
			return Task.FromResult(Guid.NewGuid().ToString());
		}

		public Task<string> Transfer(Guid requestId, Guid id, string coin, string @from, string to, decimal amount, string sign)
		{
			return Task.FromResult(Guid.NewGuid().ToString());
		}

		public Task<string> Swap(Guid requestId, Guid id, string clientA, string clientB, string coinA, string coinB, decimal amountA, decimal amountB,
			string signA, string signB)
		{
			return Task.FromResult(Guid.NewGuid().ToString());
		}
	}
}
