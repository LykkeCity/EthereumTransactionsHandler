using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{
	/// <summary>
	/// Request for signature transaction
	/// </summary>
	public interface IConfirmaionRequest
	{
		Guid RequestId { get; }

		string Client { get; }
	}

	public class ConfirmationRequest : IConfirmaionRequest
	{
		public Guid RequestId { get; set; }
		public string Client { get; set; }
	}


	public interface IConfirmationRequestRepository
	{
		Task<IConfirmaionRequest> GetConfirmationRequest(Guid requestId, string client);

		Task InsertConfirmationRequest(IConfirmaionRequest request);
	}
}
