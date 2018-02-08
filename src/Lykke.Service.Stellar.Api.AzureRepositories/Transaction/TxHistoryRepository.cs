using System;
using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;
using System.Linq;
using System.Collections.Concurrent;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public class TxHistoryRepository : ITxHistoryRepository
    {
        private static string GetPartitionKey(TxDirectionType direction) => direction.ToString();
        private static string GetLastPaymentIdRowKey() => "Last";

        private ILog _log;
        private IReloadingManager<string> _dataConnStringManager;

        private ConcurrentDictionary<string, (INoSQLTableStorage<TxHistoryEntity>, INoSQLTableStorage<IndexEntity>)> _tableCache;

        public TxHistoryRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _dataConnStringManager = dataConnStringManager;
            _log = log;
            _tableCache = new ConcurrentDictionary<string, (INoSQLTableStorage<TxHistoryEntity>, INoSQLTableStorage<IndexEntity>)>();
        }

        private string GetTableName(string tableId)
        {
            var tableName = $"TransactionHistory{tableId}";
            return tableName;
        }

        private (INoSQLTableStorage<TxHistoryEntity>, INoSQLTableStorage<IndexEntity>) GetTable(string tableId)
        {
            var tableName = GetTableName(tableId);
            if(_tableCache.ContainsKey(tableName))
            {
                return _tableCache[tableName];
            }
            var table = AzureTableStorage<TxHistoryEntity>.Create(_dataConnStringManager, tableName, _log);
            var tableIndex = AzureTableStorage<IndexEntity>.Create(_dataConnStringManager, tableName, _log);
            _tableCache.TryAdd(tableName, (table, tableIndex));
            return (table, tableIndex);
        }

        public async Task<(List<TxHistory> Items, string ContinuationToken)> GetAllAsync(string tableId, TxDirectionType direction, int take, string continuationToken)
        {
            var (table, tableIndex) = GetTable(tableId);
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, direction.ToString());
            var query = new TableQuery<TxHistoryEntity>().Where(filter).Take(take);
            var data = await table.GetDataWithContinuationTokenAsync(query, continuationToken);
            var items = data.Entities.ToDomain();
            return (items, data.ContinuationToken);
        }

        public async Task<List<TxHistory>> GetAllAfterHashAsync(string tableId, TxDirectionType direction, int take, string afterHash)
        {
            var (table, tableIndex) = GetTable(tableId);

            // build range query
            string filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, direction.ToString());
            if (!string.IsNullOrEmpty(afterHash)) 
            {
                var index = await tableIndex.GetDataAsync(IndexEntity.GetPartitionKeyHash(), afterHash);
                if (index == null)
                {
                    throw new ArgumentException($"Unknwon transaction hash: {afterHash}", "afterHash");
                }
                var rowKeys = index.Value.Split(";").ToList().OrderByDescending(x => UInt64.Parse(x));
                var rowKey = rowKeys.First();

                var rkFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, rowKey);
                filter = TableQuery.CombineFilters(filter, TableOperators.And, rkFilter);
            }

            var query = new TableQuery<TxHistoryEntity>().Where(filter).Take(take);
            var data = await table.GetDataWithContinuationTokenAsync(query, null);
            var items = data.Entities.ToDomain();
            return items;
        }

        public async Task<TxHistory> GetLastRecordAsync(string tableId)
        {
            var (table, tableIndex) = GetTable(tableId);

            var paymentId = await tableIndex.GetDataAsync(IndexEntity.GetPartitionKeyPaymentId(), GetLastPaymentIdRowKey());
            if (paymentId != null)
            {
                var entity = await table.GetDataAsync(GetPartitionKey(TxDirectionType.Incoming), paymentId.Value);
                if (entity == null)
                {
                    entity = await table.GetDataAsync(GetPartitionKey(TxDirectionType.Outgoing), paymentId.Value);
                }
                if (entity != null)
                {
                    var history = entity.ToDomain();
                    return history;
                }
            }

            return null;
        }

        public async Task InsertOrReplaceAsync(string tableId, TxDirectionType direction, TxHistory history)
        {
            var (table, tableIndex) = GetTable(tableId);

            // history entry
            var entity = history.ToEntity(GetPartitionKey(direction));
            await table.InsertOrReplaceAsync(entity);

            // hash to payments index
            var value = entity.RowKey;
            var index = await tableIndex.GetDataAsync(IndexEntity.GetPartitionKeyHash(), entity.Hash);
            if (index == null || string.IsNullOrEmpty(index.Value))
            {
                index = new IndexEntity
                {
                    PartitionKey = IndexEntity.GetPartitionKeyHash(),
                    RowKey = history.Hash,
                    Value = value
                };
            }
            else if (!index.Value.Contains(value))
            {
                index.Value += ";" + value;
            }
            await tableIndex.InsertOrReplaceAsync(index);

            // index to latest payment
            var paymentId = new IndexEntity
            {
                PartitionKey = IndexEntity.GetPartitionKeyPaymentId(),
                RowKey = GetLastPaymentIdRowKey(),
                Value = entity.RowKey
            };
            await tableIndex.InsertOrReplaceAsync(paymentId);
        }

        public async Task DeleteAsync(string tableId)
        {
            var tableName = GetTableName(tableId);
            var (table, tableIndex) = GetTable(tableName);
            await table.DeleteAsync();

            // remove from cache
            (INoSQLTableStorage<TxHistoryEntity>, INoSQLTableStorage<IndexEntity>) ignored;
            _tableCache.TryRemove(tableName, out ignored);
        }
    }
}
