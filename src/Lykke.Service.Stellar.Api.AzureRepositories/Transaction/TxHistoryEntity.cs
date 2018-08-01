using System;
using Lykke.AzureStorage.Tables;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public class TxHistoryEntity : AzureTableEntity
    {
        public string FromAddress { get; set; }

        public string ToAddress { get; set; }

        public string AssetId { get; set; }

        public long Amount { get; set; }

        public string Hash { get; set; }

        public DateTime CreatedAt { get; set; }

        public PaymentType PaymentType { get; set; }

        public string Memo => RowKey;
    }
}
