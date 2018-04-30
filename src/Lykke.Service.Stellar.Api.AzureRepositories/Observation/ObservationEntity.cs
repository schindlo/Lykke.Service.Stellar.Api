using Lykke.AzureStorage.Tables;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    public abstract class ObservationEntity<T> : AzureTableEntity
    {
        public abstract string GetRowKey(T observation);

        public abstract T ToDomain();

        public virtual void ToEntity(T observation)
        {
            RowKey = GetRowKey(observation);
        }
    }
}
