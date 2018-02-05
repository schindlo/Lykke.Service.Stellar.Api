using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    public static class Mapping
    {
        public static BalanceObservation ToDomain(this ObservationEntity entity)
        {
            var observation = new BalanceObservation
            {
                Address = entity.RowKey
            };
            return observation;
        }
    }
}
