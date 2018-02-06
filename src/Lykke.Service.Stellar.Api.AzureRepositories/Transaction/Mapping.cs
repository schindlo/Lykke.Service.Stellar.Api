using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public static class Mapping
    {
        public static TxHistory ToDomain(this TxHistoryEntity entity)
        {
            var domain = new TxHistory
            {
                OperationId = entity.OperationId,
                FromAddress = entity.FromAddress,
                ToAddress = entity.ToAddress,
                AssetId = entity.AssetId,
                Amount = entity.Amount,
                Hash = entity.Hash,
                PaymentOperationId = entity.PaymentOperationId,
                CreatedAt = entity.CreatedAt,
                PaymentType = entity.PaymentType,
                Memo = entity.Memo
            };
            return domain;
        }

        public static TxHistoryEntity ToEntity(this TxHistory domain, string partitionKey, string rowKey)
        {
            var entity = new TxHistoryEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                FromAddress = domain.FromAddress,
                ToAddress = domain.ToAddress,
                AssetId = domain.AssetId,
                Amount = domain.Amount,
                Hash = domain.Hash,
                PaymentOperationId = domain.PaymentOperationId,
                CreatedAt = domain.CreatedAt,
                PaymentType = domain.PaymentType,
                Memo = domain.Memo
            };
            return entity;
        }
    }
}
