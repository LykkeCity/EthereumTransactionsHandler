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
		/// Used to notify external services about events in coin contracts
		/// </summary>
		public const string CoinEventQueue = "ethereum-coin-event-queue";
		
		//coin contract event names
		public const string CashInAction = "CashIn";
		public const string CashOutAction = "CashOut";
		public const string SwapAction = "Swap";



		public const string MonitoringTable = "MonitoringTable";
	}
}
