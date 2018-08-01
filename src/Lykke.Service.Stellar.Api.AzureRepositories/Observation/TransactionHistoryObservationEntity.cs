using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    public class TransactionHistoryObservationEntity : ObservationEntity<TransactionHistoryObservation>
    {
        public const string TableName = "TransactionHistoryObservation";

        public bool IsIncomingTxObserved { get; set; }

        public bool IsOutgoingTxObserved { get; set; }

        public override string GetRowKey(TransactionHistoryObservation observation)
        {
            return observation.Address;
        }

        public override TransactionHistoryObservation ToDomain()
        {
            var observation = new TransactionHistoryObservation
            {
                Address = RowKey,
                IsIncomingObserved = IsIncomingTxObserved,
                IsOutgoingObserved = IsOutgoingTxObserved

            };
            return observation;
        }

        public override void ToEntity(TransactionHistoryObservation observation)
        {
            base.ToEntity(observation);
            IsIncomingTxObserved = observation.IsIncomingObserved;
            IsOutgoingTxObserved = observation.IsOutgoingObserved;
        }
    }
}
