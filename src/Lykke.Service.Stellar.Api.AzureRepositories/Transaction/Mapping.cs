using System;
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
                FromAddress = entity.FromAddress,
                ToAddress = entity.ToAddress,
                AssetId = entity.AssetId,
                Amount = entity.Amount,
                Hash = entity.Hash,
                CreatedAt = entity.CreatedAt,
                PaymentType = entity.PaymentType,
                Memo = entity.Memo,
            };
            return domain;
        }

        public static TxHistoryEntity ToEntity(this TxHistory domain, string partitionKey)
        {
            var entity = new TxHistoryEntity
            {
                PartitionKey = partitionKey,
                RowKey = UInt64.Parse(domain.PagingToken).ToString("D20") + domain.OperationIndex.ToString("D3"),
                FromAddress = domain.FromAddress,
                ToAddress = domain.ToAddress,
                AssetId = domain.AssetId,
                Amount = domain.Amount,
                Hash = domain.Hash,
                CreatedAt = domain.CreatedAt,
                PaymentType = domain.PaymentType,
                Memo = domain.Memo
            };
            return entity;
        }

        public static TxBroadcast ToDomain(this TxBroadcastEntity entity)
        {
            var domain = new TxBroadcast
            {
                OperationId = Guid.Parse(entity.RowKey),
                State = entity.State,
                Amount = entity.Amount,
                Fee = entity.Fee,
                Hash = entity.Hash,
                Ledger = entity.Ledger,
                CreatedAt = entity.CreatedAt,
                Error = entity.Error,
                ErrorCode = entity.ErrorCode
            };
            return domain;
        }

        public static TxBroadcastEntity ToEntity(this TxBroadcast domain, string partitionKey, string rowKey)
        {
            var entity = new TxBroadcastEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                State = domain.State,
                Amount = domain.Amount,
                Fee = domain.Fee,
                Hash = domain.Hash,
                Ledger = domain.Ledger,
                CreatedAt = domain.CreatedAt,
                Error = domain.Error,
                ErrorCode = domain.ErrorCode
            };
            return entity;
        }
    }
}
