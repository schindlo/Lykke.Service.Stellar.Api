using Lykke.AzureStorage.Tables;

namespace Lykke.Service.Stellar.Api.AzureRepositories
{
    public class IndexEntity : AzureTableEntity
    {
        public static string GetPartitionKeyHash() => "Hash";
        public static string GetPartitionKeyPagingToken() => "PagingToken";

        public string Value { get; set; }
    }
}
