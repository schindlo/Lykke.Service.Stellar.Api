using System;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    public class BroadcastObservationEntity : ObservationEntity<BroadcastObservation>
    {
        public override BroadcastObservation ToDomain()
        {
            var observation = new BroadcastObservation
            {
                OperationId = Guid.Parse(RowKey)
            };
            return observation;
        }

        public override void ToEntity(BroadcastObservation observation)
        {
            RowKey = observation.OperationId.ToString();
        }
    }
}