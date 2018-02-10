using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class IndexEntity : AzureTableEntity
    {
        public static string GetPartitionKeyHash() => "Hash";
        public static string GetPartitionKeyPaymentId() => "PaymentId";

        public string Value { get; set; }
    }
}
