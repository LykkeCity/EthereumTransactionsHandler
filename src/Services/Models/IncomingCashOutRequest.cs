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
	public class IncomingCashOutRequest
	{
		public Guid TransactionId { get; set; }

		public string Coin { get; set; }

		public string Client { get; set; }

		public string To { get; set; }

		public decimal Amount { get; set; }

		public string Sign { get; set; }

		public async Task<string> BuildHash(ICoinRepository coinRepository)
		{
			var coin = await coinRepository.GetCoin(Coin);
			var str = EthUtils.GuidToByteArray(TransactionId).ToHex() +
					   coin.AssetAddress.HexToByteArray().ToHex() +
					   Client.HexToByteArray().ToHex() +
					   To.HexToByteArray().ToHex() +
					   EthUtils.BigIntToArrayWithPadding(Amount.ToBlockchainAmount(coin.Multiplier)).ToHex();
			return new Sha3Keccack().CalculateHash(str.HexToByteArray()).ToHex(true);
		}
	}
}