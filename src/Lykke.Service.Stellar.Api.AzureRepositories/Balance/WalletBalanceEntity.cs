using Lykke.AzureStorage.Tables;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Balance
{
    public class WalletBalanceEntity : AzureTableEntity
    {
        public string AssetId
        {
            get => PartitionKey;
        }

        public string Address
        {
            get => RowKey;
        }

        public long Balance { get; set; }

        public long Ledger { get; set; }

        public int OperationCount { get; set; }
    }
}