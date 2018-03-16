using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
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