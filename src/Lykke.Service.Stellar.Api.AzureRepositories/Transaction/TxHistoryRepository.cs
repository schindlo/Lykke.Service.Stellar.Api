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
        private static string GetPartitionKeyHashIndex() => "HashIndex";
        private static string GetPartitionKeySequence() => "Sequence";

        private ILog _log;
        private IReloadingManager<string> _dataConnStringManager;

        private ConcurrentDictionary<string, INoSQLTableStorage<TxHistoryEntity>> _tableCache;

        public TxHistoryRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _dataConnStringManager = dataConnStringManager;
            _log = log;
            _tableCache = new ConcurrentDictionary<string, INoSQLTableStorage<TxHistoryEntity>>();
        }

        private INoSQLTableStorage<TxHistoryEntity> GetTable(string address)
        {
            var tableName = $"Account{address}";
            if(_tableCache.ContainsKey(tableName))
            {
                return _tableCache[tableName];
            }
            var table = AzureTableStorage<TxHistoryEntity>.Create(_dataConnStringManager, tableName, _log);
            _tableCache[tableName] = table;
            return table;
        }

        public async Task<(List<TxHistory> Items, string ContinuationToken)> GetAllAsync(TxDirectionType direction, string address, int take, string continuationToken)
        {
            var table = GetTable(address);
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, direction.ToString());
            var query = new TableQuery<TxHistoryEntity>().Where(filter).Take(take);
            var data = await table.GetDataWithContinuationTokenAsync(query, continuationToken);
            var items = data.Entities.ToDomain();
            return (items, data.ContinuationToken);
        }

        public async Task<List<TxHistory>> GetAllAfterHashAsync(TxDirectionType direction, string address, int take, string afterHash)
        {
            var table = GetTable(address);

            // build range query
            string filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, direction.ToString());
            if (!string.IsNullOrEmpty(afterHash)) 
            {
                var index = await table.GetDataAsync(GetPartitionKeyHashIndex(), afterHash);
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

        public async Task<TxHistory> GetLastRecordAsync(string address)
        {
            var table = GetTable(address);

            var seq = await table.GetDataAsync(GetPartitionKeySequence(), "Current");
            if (seq != null)
            {
                var entity = await table.GetDataAsync(GetPartitionKey(TxDirectionType.Incoming), seq.Value);
                if (entity == null)
                {
                    entity = await table.GetDataAsync(GetPartitionKey(TxDirectionType.Outgoing), seq.Value);
                }
                if (entity != null)
                {
                    var history = entity.ToDomain();
                    return history;
                }
            }

            return null;
        }

        public async Task InsertOrReplaceAsync(TxDirectionType direction, TxHistory history)
        {
            var address = direction == TxDirectionType.Outgoing ? history.FromAddress : history.ToAddress;
            var table = GetTable(address);

            // history entry
            var entity = history.ToEntity(GetPartitionKey(direction));
            await table.InsertOrReplaceAsync(entity);

            // hash to payments index
            var value = entity.RowKey;
            var index = await table.GetDataAsync(GetPartitionKeyHashIndex(), entity.Hash);
            if (index == null || string.IsNullOrEmpty(index.Value))
            {
                index = new TxHistoryEntity
                {
                    PartitionKey = GetPartitionKeyHashIndex(),
                    RowKey = history.Hash,
                    Value = value
                };
            }
            else if (!index.Value.Contains(value))
            {
                index.Value += ";" + value;
            }
            await table.InsertOrReplaceAsync(index);

            // index to latest payment
            var seq = new TxHistoryEntity
            {
                PartitionKey = GetPartitionKeySequence(),
                RowKey = "Current",
                Value = entity.RowKey
            };
            await table.InsertOrReplaceAsync(seq);
        }

        public async Task DeleteAsync(string address)
        {
            var tableName = $"Account{address}";
            var table = GetTable(address);
            await table.DeleteAsync();

            // remove from cache
            INoSQLTableStorage<TxHistoryEntity> ignored;
            _tableCache.Remove(tableName, out ignored);
        }
    }
}
