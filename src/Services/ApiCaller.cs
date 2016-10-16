using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.Log;
using Core.Settings;
using Core.Utils;
using Newtonsoft.Json;
using RestSharp;

namespace Services
{

	public interface IApiCaller
	{
		Task<string> Cashin(Guid requestId, Guid id, string coin, string to, decimal amount);
		Task<string> Cashout(Guid requestId, Guid id, string coin, string client, string to, decimal amount, string sign);

		Task<string> Swap(Guid requestId, Guid id, string clientA, string clientB, string coinA, string coinB, decimal amountA,
			decimal amountB, string signA, string signB);
	}

	public class ApiCaller : IApiCaller
	{
		private readonly IRestClient _restClient;
		private readonly IBaseSettings _baseSettings;
		private readonly ILog _logger;

		private static int _requestId = 0;

		public ApiCaller(IRestClient restClient, IBaseSettings baseSettings, ILog logger)
		{
			_restClient = restClient;
			_baseSettings = baseSettings;
			_logger = logger;
			_restClient.BaseUrl = new Uri(baseSettings.ApiUrl);
		}


		public async Task<T> DoRequest<T>(RestRequest request) where T : new()
		{
			int reqId = Interlocked.Increment(ref _requestId);

			var info = new StringBuilder();
			info.Append($"Invoke request reqId={reqId}, Method: {request.Method} {request.Resource}, Params: {Environment.NewLine}");
			foreach (var parameter in request.Parameters)
				info.Append(parameter.Name + "=" + parameter.Value + Environment.NewLine);
			await _logger.WriteInfo("ApiCaller", "DoRequest", "", info.ToString());

			var t = new TaskCompletionSource<IRestResponse>();
			_restClient.ExecuteAsync(request, resp => { t.SetResult(resp); });
			var response = await t.Task;

			if (response.ResponseStatus == ResponseStatus.Completed && response.StatusCode == HttpStatusCode.OK)
			{
				var content = response.Content;
				await _logger.WriteInfo("ApiCaller", "DoRequest", "", $"Response reqId={reqId}: {content} ");
				return response.Content.DeserializeJson<T>();
			}
			var exception = response.ErrorException ?? new Exception(response.Content);
			await _logger.WriteError("ApiCaller", "DoRequest", $"reqId={reqId}", exception);
			throw exception;
		}

		public async Task<string> Cashin(Guid requestId, Guid id, string coin, string to, decimal amount)
		{
			var request = new RestRequest("/api/coin/cashin", Method.POST);
			request.AddJsonBody(new
			{
				id = id,
				coin = coin,
				receiver = to,
				amount = amount,
				requestId = requestId
			});

			return (await DoRequest<TransactionResponse>(request)).TransactionHash;
		}

		public async Task<string> Cashout(Guid requestId, Guid id, string coin, string client, string to, decimal amount, string sign)
		{
			var request = new RestRequest("/api/coin/cashout", Method.POST);
			request.AddJsonBody(new
			{
				id = id,
				coin = coin,
				client = client,
				to = to,
				amount = amount,
				sign = sign,
				requestId = requestId
			});
			return (await DoRequest<TransactionResponse>(request)).TransactionHash;

		}

		public async Task<string> Swap(Guid requestId, Guid id, string clientA, string clientB, string coinA, string coinB, decimal amountA, decimal amountB,
			string signA, string signB)
		{
			var request = new RestRequest("/api/coin/swap", Method.POST);
			request.AddJsonBody(new
			{
				id = id,
				clientA = clientA,
				clientB = clientB,
				coinA = coinA,
				coinB = coinB,
				amountA = amountA,
				amountB = amountB,
				signA = signA,
				signB = signB,
				requestId = requestId
			});
			return (await DoRequest<TransactionResponse>(request)).TransactionHash;
		}
	}

	public class TransactionResponse
	{
		public string TransactionHash { get; set; }
	}
}
