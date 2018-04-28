using Common;
using Lykke.AzureStorage.Tables;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    public abstract class ObservationEntity<T> : AzureTableEntity
    {
        public static string GetPartitionKey(string rowKey)
        {
            // Use hash to distribute all records to the different partitions
            var hash = rowKey.CalculateHexHash32(3);
            return $"{hash}";
        }

        public abstract string GetRowKey(T observation);

        public abstract T ToDomain();

        public virtual void ToEntity(T observation)
        {
            RowKey = GetRowKey(observation);
        }
    }
}