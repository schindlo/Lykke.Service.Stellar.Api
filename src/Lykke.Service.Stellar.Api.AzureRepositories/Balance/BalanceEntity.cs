using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Balance
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class BalanceEntity : AzureTableEntity
    {
        public string AssetId
        {
            get => PartitionKey.Split(":")[0];
        }

        public string Address
        {
            get => PartitionKey.Split(":")[1];
        }

        public string DestinationTag
        {
            get => RowKey;
        }

        public long Balance { get; set; }

        public long Ledger { get; set; }
    }
}
