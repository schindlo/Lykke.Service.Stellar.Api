using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class TxHistoryEntity : AzureTableEntity
    {
        public ulong InverseSequence 
        {
            get => UInt64.Parse(RowKey); 
        }

        public Guid? OperationId { get; set; }

        public string FromAddress { get; set; }

        public string ToAddress { get; set; }

        public string AssetId { get; set; }

        public long? Amount { get; set; }

        public string Hash { get; set; }

        public string PaymentOperationId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public PaymentType? PaymentType { get; set; }

        public string Memo { get; set; }

        public string Value { get; set; }
    }
}
