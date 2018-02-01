using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class TxBroadcastEntity : AzureTableEntity
    {
        public Guid OperationId
        {
            get => Guid.Parse(RowKey);
        }

        public TxBroadcastState State { get; set; }

        public string Hash { get; set; }
    }
}