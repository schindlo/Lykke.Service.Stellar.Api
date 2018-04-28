using Lykke.AzureStorage.Tables;

namespace Lykke.Service.Stellar.Api.AzureRepositories
{
    public class KeyValueEntity : AzureTableEntity
    {
        public string Value { get; set; }
    }
}
