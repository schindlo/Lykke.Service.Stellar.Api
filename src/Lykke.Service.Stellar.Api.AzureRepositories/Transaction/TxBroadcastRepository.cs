﻿using System;
using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public class TxBroadcastRepository : ITxBroadcastRepository
    {
        private const string TableName = "Transaction";

        private static string GetPartitionKey() => "Broadcast";
        private static string GetRowKey(Guid operationId) => operationId.ToString();

        private INoSQLTableStorage<TxBroadcastEntity> _table;
        private INoSQLTableStorage<IndexEntity> _tableIndex;

        public TxBroadcastRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _table = AzureTableStorage<TxBroadcastEntity>.Create(dataConnStringManager, TableName, log);
            _tableIndex = AzureTableStorage<IndexEntity>.Create(dataConnStringManager, TableName, log);
        }

        public async Task<TxBroadcast> GetAsync(Guid operationId)
        {
            var entity = await _table.GetDataAsync(GetPartitionKey(), GetRowKey(operationId));
            if (entity != null)
            {
                var broadcast = entity.ToDomain();
                return broadcast;
            }

            return null;
        }

        public async Task<Guid?> GetOperationId(string hash)
        {
            var index = await _tableIndex.GetDataAsync(IndexEntity.GetPartitionKeyHash(), hash);
            if (index != null)
            {
                return Guid.Parse(index.Value);
            }

            return null;
        }

        public async Task InsertOrReplaceAsync(TxBroadcast broadcast)
        {
            var entity = broadcast.ToEntity(GetPartitionKey(), GetRowKey(broadcast.OperationId));
            await _table.InsertOrReplaceAsync(entity);
            // add index
            if (!string.IsNullOrEmpty(broadcast.Hash))
            {
                var index = new IndexEntity
                {
                    PartitionKey = IndexEntity.GetPartitionKeyHash(),
                    RowKey = broadcast.Hash,
                    Value = entity.RowKey
                };
                await _tableIndex.InsertOrReplaceAsync(index);
            }
        }

        public async Task MergeAsync(TxBroadcast broadcast)
        {
            TxBroadcastEntity MergeAction(TxBroadcastEntity entity)
            {
                entity.State = broadcast.State;
                entity.Amount = broadcast.Amount;
                entity.Fee = broadcast.Fee;
                entity.Ledger = broadcast.Ledger;
                entity.CreatedAt = broadcast.CreatedAt;
                entity.Error = broadcast.Error;
                entity.ErrorCode = broadcast.ErrorCode;

                return entity;
            }

            await _table.MergeAsync(GetPartitionKey(), GetRowKey(broadcast.OperationId), MergeAction);
        }

        public async Task DeleteAsync(Guid operationId)
        {
            var entity = await _table.DeleteAsync(GetPartitionKey(), GetRowKey(operationId));
            // delete index
            if (entity != null && !string.IsNullOrEmpty(entity.Hash))
            {
                await _tableIndex.DeleteIfExistAsync(IndexEntity.GetPartitionKeyHash(), entity.Hash);
            }
        }
    }
}
