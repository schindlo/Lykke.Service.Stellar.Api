using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Balance
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
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
    }
}
