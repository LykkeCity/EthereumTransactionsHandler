using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Repositories
{
    public class ConfirmationRequestEntity : TableEntity, IConfirmaionRequest
    {
        public string Client => PartitionKey;
        public Guid RequestId => new Guid(RowKey);

        public ConfirmationRequestEntity()
        {

        }

        public ConfirmationRequestEntity(string client, Guid requestId)
        {
            PartitionKey = client;
            RowKey = requestId.ToString();
        }
    }



    public class ConfirmationRequestRepository : IConfirmationRequestRepository
    {
        private readonly INoSQLTableStorage<ConfirmationRequestEntity> _table;

        public ConfirmationRequestRepository(INoSQLTableStorage<ConfirmationRequestEntity> table)
        {
            _table = table;
        }

        public async Task<IConfirmaionRequest> GetConfirmationRequest(Guid requestId, string client)
        {
            return await _table.GetDataAsync(client, requestId.ToString());
        }

        public Task InsertConfirmationRequest(IConfirmaionRequest request)
        {
            return _table.InsertAsync(new ConfirmationRequestEntity(request.Client, request.RequestId));
        }

        public void DeleteTable()
        {
            _table.DeleteIfExists();
        }
    }
}
