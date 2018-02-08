namespace Lykke.Service.Stellar.Api.Core.Domain.Observation
{
    public class TransactionHistoryObservation
    {
        public bool IsIncomingObserved { get; set; }

        public bool IsOutgoingObserved { get; set; }

        public string Address { get; set; }

        public string TableId { get; set; }
    }
}
