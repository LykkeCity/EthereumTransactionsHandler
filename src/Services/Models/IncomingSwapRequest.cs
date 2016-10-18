using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Repositories;
using Core.Utils;
using Nethereum.ABI.Util;
using Nethereum.Hex.HexConvertors.Extensions;

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

		public async Task<string> BuildHash(ICoinRepository coinRepository)
		{
			var coinA = await coinRepository.GetCoin(CoinA);
			var coinB = await coinRepository.GetCoin(CoinB);

			var strForHash3 = EthUtils.GuidToByteArray(TransactionId).ToHex() +
							ClientA.HexToByteArray().ToHex() +
							ClientB.HexToByteArray().ToHex() +
							CoinA.HexToByteArray().ToHex() +
							CoinB.HexToByteArray().ToHex() +
							EthUtils.BigIntToArrayWithPadding(AmountA.ToBlockchainAmount(coinA.Multiplier)).ToHex() +
							EthUtils.BigIntToArrayWithPadding(AmountB.ToBlockchainAmount(coinB.Multiplier)).ToHex();
			return new Sha3Keccack().CalculateHash(strForHash3.HexToByteArray()).ToHex(true);

		}
	}
}
