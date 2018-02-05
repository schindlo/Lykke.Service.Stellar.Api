using System;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class TransactionObservationEntity : ObservationEntity<TransactionObservation>
    {
        public bool IsIncomingTxObserved { get; set; }

        public bool IsOutgoingTxObserved { get; set; }

        public override TransactionObservation ToDomain()
        {
            var observation = new TransactionObservation
            {
                Address = RowKey,
                IsIncomingObserved = IsIncomingTxObserved,
                IsOutgoingObserved = IsOutgoingTxObserved
            };
            return observation;
        }

        public override void ToEntity(TransactionObservation observation)
        {
            RowKey = observation.Address;
            IsIncomingTxObserved = observation.IsIncomingObserved;
            IsOutgoingTxObserved = observation.IsOutgoingObserved;
        }
    }
}
