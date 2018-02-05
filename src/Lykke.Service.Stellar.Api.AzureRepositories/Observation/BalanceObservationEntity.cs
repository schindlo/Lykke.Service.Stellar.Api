using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class BalanceObservationEntity : ObservationEntity<BalanceObservation>
    {
        public override BalanceObservation ToDomain()
        {
            var observation = new BalanceObservation
            {
                Address = RowKey
            };
            return observation;
        }

        public override void ToEntity(BalanceObservation observation)
        {
            RowKey = observation.Address;
        }
    }
}