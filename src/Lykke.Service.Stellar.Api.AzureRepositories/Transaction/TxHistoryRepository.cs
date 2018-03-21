using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public class TxHistoryRepository : ITxHistoryRepository
    {
        private const string TableNamePrefix = "TransactionHistory";
        private const string IndexSeparator = ";";

        private static string GetPartitionKey(TxDirectionType direction) => direction.ToString();
        private static string GetCurrentRowKey() => "Current";

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
            var tableName = $"{TableNamePrefix}{tableId}";
            return tableName;
        }

        private (INoSQLTableStorage<TxHistoryEntity>, INoSQLTableStorage<IndexEntity>) GetTable(string tableId)
        {
            var tableName = GetTableName(tableId);
            if (_tableCache.ContainsKey(tableName))
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
            var filter = TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, direction.ToString());
            var query = new TableQuery<TxHistoryEntity>().Where(filter).Take(take);
            var data = await table.GetDataWithContinuationTokenAsync(query, continuationToken);
            var items = data.Entities.ToDomain();
            return (items, data.ContinuationToken);
        }

        public async Task<List<TxHistory>> GetAllAfterHashAsync(string tableId, TxDirectionType direction, int take, string afterHash)
        {
            var (table, tableIndex) = GetTable(tableId);

            // build range query
            string filter = TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, direction.ToString());
            if (!string.IsNullOrEmpty(afterHash))
            {
                var index = await tableIndex.GetDataAsync(IndexEntity.GetPartitionKeyHash(), afterHash);
                if (index == null)
                {
                    throw new ArgumentException($"Unknwon transaction hash: {afterHash}", nameof(afterHash));
                }
                var rowKeys = index.Value.Split(IndexSeparator).ToList().OrderByDescending(x => x);
                var rowKey = rowKeys.First();

                var rkFilter = TableQuery.GenerateFilterCondition(nameof(ITableEntity.RowKey), QueryComparisons.GreaterThan, rowKey);
                filter = TableQuery.CombineFilters(filter, TableOperators.And, rkFilter);
            }

            var query = new TableQuery<TxHistoryEntity>().Where(filter).Take(take);
            var data = await table.GetDataWithContinuationTokenAsync(query, null);
            var items = data.Entities.ToDomain();
            return items;
        }

        public async Task<string> GetCurrentPagingToken(string tableId)
        {
            var (table, tableIndex) = GetTable(tableId);

            var pagingTokenIndex = await tableIndex.GetDataAsync(IndexEntity.GetPartitionKeyPagingToken(), GetCurrentRowKey());
            if (pagingTokenIndex != null)
            {
                return pagingTokenIndex.Value;
            }

            return null;
        }

        public async Task SetCurrentPagingToken(string tableId, string pagingToken)
        {
            var (table, tableIndex) = GetTable(tableId);

            // index to current paging token
            var entity = new IndexEntity
            {
                PartitionKey = IndexEntity.GetPartitionKeyPagingToken(),
                RowKey = GetCurrentRowKey(),
                Value = pagingToken
            };
            await tableIndex.InsertOrReplaceAsync(entity);
        }

        public async Task InsertOrReplaceAsync(string tableId, TxDirectionType direction, TxHistory history)
        {
            var (table, tableIndex) = GetTable(tableId);

            var tasks = new Task[2];
                
            // history entry
            var entity = history.ToEntity(GetPartitionKey(direction));
            tasks[0] = table.InsertOrReplaceAsync(entity);

            // hash to row key(s)
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
                index.Value += IndexSeparator + value;
            }
            tasks[1] = tableIndex.InsertOrReplaceAsync(index);

            await Task.WhenAll(tasks);
        }

        public async Task DeleteAsync(string tableId)
        {
            var (table, tableIndex) = GetTable(tableId);
            await table.DeleteAsync();

            // remove from cache
            var tableName = GetTableName(tableId);
            (INoSQLTableStorage<TxHistoryEntity>, INoSQLTableStorage<IndexEntity>) ignored;
            _tableCache.TryRemove(tableName, out ignored);
        }
    }
}
