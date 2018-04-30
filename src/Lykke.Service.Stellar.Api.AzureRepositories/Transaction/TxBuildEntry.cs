using System;
using Lykke.AzureStorage.Tables;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public class TxBuildEntity : AzureTableEntity
    {
        public Guid OperationId => Guid.Parse(RowKey);

        public string XdrBase64 { get; set; }
    }
}
