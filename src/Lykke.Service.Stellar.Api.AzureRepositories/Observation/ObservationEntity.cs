using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public abstract class ObservationEntity<T> : AzureTableEntity
    {
        public abstract T ToDomain();

        public abstract void ToEntity(T observation);
    }
}