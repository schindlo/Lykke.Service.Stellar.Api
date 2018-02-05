using System;
namespace Lykke.Service.Stellar.Api.Core.Domain.Observation
{
    public class TransactionObservation
    {
        public bool IsIncomingObserved { get; set; }

        public bool IsOutgoingObserved { get; set; }

        public string Address { get; set; }
    }
}
