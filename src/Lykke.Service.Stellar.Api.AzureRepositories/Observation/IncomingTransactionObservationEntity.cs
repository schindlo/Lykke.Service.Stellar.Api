using System;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class IncomingTransactionObservationEntity : ObservationEntity<IncomingTransactionObservation>
    {
        public override IncomingTransactionObservation ToDomain()
        {
            var observation = new IncomingTransactionObservation
            {
                Address = RowKey,
            };
            return observation;
        }

        public override void ToEntity(IncomingTransactionObservation observation)
        {
            RowKey = observation.Address;
        }
    }
}
