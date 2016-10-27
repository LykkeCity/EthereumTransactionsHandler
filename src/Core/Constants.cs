namespace Core
{
	public class Constants
	{
		/// <summary>
		/// Used to change table and queue names in testing enviroment
		/// </summary>
		public static string StoragePrefix { get; set; } = "";

		


		public const string EmailNotifierQueue = "emailsqueue";		

		/// <summary>
		/// Used to get status of coin transactions
		/// </summary>
		public const string CoinTransactionQueue = "ethereum-coin-transaction-queue";

		/// <summary>
		/// Used to listening incoming requests from extrnal service
		/// </summary>
		public const string CoinIncomingRequestsQueue = "ethereum-coin-request-queue";

		/// <summary>
		/// Used to determine if we should setup client queue
		/// </summary>
		public const string ClientQueuePrefix = "ethereum-client-queue";
		

		/// <summary>
		/// Used to notify external services about events in coin contracts
		/// </summary>
		public const string CoinEventQueue = "ethereum-coin-event-queue";

		/// <summary>
		/// Used to request signature from client
		/// </summary>
		public const string ConfirmationRequestQueue = "confirmation-request-queue";

		/// <summary>
		/// Used to get signature from client
		/// </summary>
		public const string ConfirmationResponseQueue = "ethereum-signed-request-queue";


		public const string MonitoringTable = "MonitoringTable";
		public const string CoinTransactionTable = "CoinTransactionTable";
		public const string QueueListenerTable = "QueueListenerTable";
		public const string TransactionRequestMappingTable = "TransactionRequestMappingTable";
		public const string CoinTable = "CoinTable";
		public const string ConfirmationRequestTable = "ConfirmationRequestTable";

		public const string EthereumBlockchain = "Ethereum";
	}
}
