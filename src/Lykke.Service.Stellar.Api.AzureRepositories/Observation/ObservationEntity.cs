using Lykke.AzureStorage.Tables;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    public abstract class ObservationEntity<T> : AzureTableEntity
    {
        public abstract T ToDomain();

        public abstract void ToEntity(T observation);
    }
}