using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateAlways)]
    public class TxBuildEntity : AzureTableEntity
    {
        public Guid OperationId
        {
            get => Guid.Parse(RowKey);
        }

        public string XdrBase64 { get; set; }
    }
}