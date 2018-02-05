using System;

namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public class TxHistory
    {
        public Guid? OperationId { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string FromAddress { get; set; }

        public string ToAddress { get; set; }

        public string AssetId { get; set; }

        public long Amount { get; set; }

        public string Hash { get; set; }

        public ulong PaymentOperationId { get; set; }
    }
}
