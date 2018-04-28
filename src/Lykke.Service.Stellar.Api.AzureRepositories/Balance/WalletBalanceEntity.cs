using Lykke.AzureStorage.Tables;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Balance
{
    public class WalletBalanceEntity : AzureTableEntity
    {
        public string AssetId
        {
            get => RowKey.Split(':')[0];
        }

        public string Address
        {
            get => RowKey.Split(':')[1];
        }

        public long Balance { get; set; }

        public long Ledger { get; set; }

        public int OperationCount { get; set; }

        public static string GetRowKey(string assetId, string address)
        {
            return $"{assetId}:{address}";
        }
    }
}