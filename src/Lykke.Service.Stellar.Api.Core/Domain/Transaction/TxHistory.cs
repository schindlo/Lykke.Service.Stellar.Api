using System;

namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public class TxHistory
    {
        public string FromAddress { get; set; }

        public string ToAddress { get; set; }

        public string AssetId { get; set; }

        public long Amount { get; set; }

        public string Hash { get; set; }

        public short OperationIndex { get; set; }

        public string PagingToken { get; set; }

        public DateTime CreatedAt { get; set; }

        public PaymentType PaymentType { get; set; }

        public string Memo { get; set; }
    }
}