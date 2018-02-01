using System;

namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public class TxBroadcast
    {
        public Guid OperationId { get; set; }

        public TxBroadcastState State { get; set; }

        public string Hash { get; set; }
    }
}
