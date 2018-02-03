using System;

namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public class TxBuild
    {
        public Guid OperationId { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string XdrBase64 { get; set; }
    }
}
