using System;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class TransactionHistoryObservationEntity : ObservationEntity<TransactionHistoryObservation>
    {
        public bool IsIncomingTxObserved { get; set; }

        public bool IsOutgoingTxObserved { get; set; }

        public string TableId { get; set; }

        public override TransactionHistoryObservation ToDomain()
        {
            var observation = new TransactionHistoryObservation
            {
                Address = RowKey,
                IsIncomingObserved = IsIncomingTxObserved,
                IsOutgoingObserved = IsOutgoingTxObserved,
                TableId = TableId

            };
            return observation;
        }

        public override void ToEntity(TransactionHistoryObservation observation)
        {
            RowKey = observation.Address;
            IsIncomingTxObserved = observation.IsIncomingObserved;
            IsOutgoingTxObserved = observation.IsOutgoingObserved;
            TableId = observation.TableId;
        }
    }
}
