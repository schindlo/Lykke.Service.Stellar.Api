using Lykke.AzureStorage.Tables;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public class IndexEntity : AzureTableEntity
    {
        public static string GetPartitionKeyHash() => "Hash";
        public static string GetPartitionKeyPaymentId() => "PaymentId";

        public string Value { get; set; }
    }
}
