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
        public TxBroadcastState State { get; set; }

        public long? Amount { get; set; }

        public long? Fee { get; set; }

        public string Hash { get; set; }

        public long? Ledger { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string Error { get; set; }

        public TxExecutionError? ErrorCode { get; set; }
    }
}