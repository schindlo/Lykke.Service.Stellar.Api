using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    public static class Mapping
    {
        public static BalanceObservation ToDomain(this ObservationEntity entity)
        {
            var observation = new BalanceObservation
            {
                Address = entity.RowKey.Split(":")[0],
                DestinationTag = entity.RowKey.Contains(":") ? entity.RowKey.Split(":")[1] : null
            };
            return observation;
        }
    }
}
