using System;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class OutgoingTransactionObservationEntity : ObservationEntity<OutgoingTransactionObservation>
    {
        public override OutgoingTransactionObservation ToDomain()
        {
            var observation = new OutgoingTransactionObservation
            {
                Address = RowKey,
            };
            return observation;
        }

        public override void ToEntity(OutgoingTransactionObservation observation)
        {
            RowKey = observation.Address;
        }
    }
}
