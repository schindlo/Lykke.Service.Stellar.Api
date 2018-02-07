using System.Collections.Generic;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public static class Mapping
    {
        public static List<TxHistory> ToDomain(this IEnumerable<TxHistoryEntity> entities)
        {
            var items = new List<TxHistory>();
            foreach (var entity in entities)
            {
                var history = entity.ToDomain();
                items.Add(history);
            }
            return items;
        }

        public static TxHistory ToDomain(this TxHistoryEntity entity)
        {
            var domain = new TxHistory
            {
                Sequence = entity.Sequence,
                OperationId = entity.OperationId,
                FromAddress = entity.FromAddress,
                ToAddress = entity.ToAddress,
                AssetId = entity.AssetId,
                Amount = entity.Amount.Value,
                Hash = entity.Hash,
                PaymentOperationId = entity.PaymentOperationId,
                CreatedAt = entity.CreatedAt.Value,
                PaymentType = entity.PaymentType.Value,
                Memo = entity.Memo,
                LastModified = entity.Timestamp
            };
            return domain;
        }

        public static TxHistoryEntity ToEntity(this TxHistory domain, string partitionKey)
        {
            var entity = new TxHistoryEntity
            {
                PartitionKey = partitionKey,
                RowKey = domain.Sequence.ToString("D20"),
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
