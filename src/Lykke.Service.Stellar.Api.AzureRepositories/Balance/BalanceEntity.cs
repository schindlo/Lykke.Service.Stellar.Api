using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Balance
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class BalanceEntity : AzureTableEntity
    {
        public Guid OperationId
        {
            get => Guid.Parse(RowKey);
        }

        public string Address { get; set; }

        public string AssetId { get; set; }

        public string Balance { get; set; }

        public long Block { get; set; }
    }
}
