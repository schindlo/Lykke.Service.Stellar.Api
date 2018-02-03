using System;

namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public class TxBroadcast
    {
        public Guid OperationId { get; set; }

        public TxBroadcastState State { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public long Amount { get; set; }

        public long Fee { get; set; }

        public string Hash { get; set; }

        public long? Ledger { get; set; }

        public string Error { get; set; }

        public TxExecutionError? ErrorCode { get; set; }
    }
}
