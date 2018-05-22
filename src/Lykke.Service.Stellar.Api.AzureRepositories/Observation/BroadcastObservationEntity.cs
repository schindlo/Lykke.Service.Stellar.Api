using System;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    public class BroadcastObservationEntity : ObservationEntity<BroadcastObservation>
    {
        public const string TableName = "BroadcastObservation";

        public override string GetRowKey(BroadcastObservation observation)
        {
            return TableKeyHelper.GetRowKey(observation.OperationId);
        }

        public override BroadcastObservation ToDomain()
        {
            var observation = new BroadcastObservation
            {
                OperationId = Guid.Parse(RowKey)
            };
            return observation;
        }
    }
}