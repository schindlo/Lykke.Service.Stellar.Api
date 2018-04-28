using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    public class BalanceObservationEntity : ObservationEntity<BalanceObservation>
    {
        public override string GetRowKey(BalanceObservation observation)
        {
            return observation.Address;
        }

        public override BalanceObservation ToDomain()
        {
            var observation = new BalanceObservation
            {
                Address = RowKey
            };
            return observation;
        }
    }
}