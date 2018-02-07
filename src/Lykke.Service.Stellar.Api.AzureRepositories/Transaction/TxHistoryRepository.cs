using System;
using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public class TxHistoryRepository : ITxHistoryRepository
    {
        private static string GetPartitionKey(TxDirectionType direction) => direction.ToString();
        private static string GetPartitionKeyHashIndex() => "HashIndex";

        private ILog _log;
        private IReloadingManager<string> _dataConnStringManager;

        public TxHistoryRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _dataConnStringManager = dataConnStringManager;
            _log = log;
        }

        private INoSQLTableStorage<TxHistoryEntity> GetTable(string address)
        {
            // TODO: cache
            var tableName = $"Account{address}";
            var table = AzureTableStorage<TxHistoryEntity>.Create(_dataConnStringManager, tableName, _log);
            return table;
        }

        public async Task<(List<TxHistory> Items, string ContinuationToken)> GetAllAsync(TxDirectionType direction, string address, int take, string continuationToken)
        {
            var table = GetTable(address);
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, direction.ToString());
            var query = new TableQuery<TxHistoryEntity>().Where(filter).Take(take);
            var data = await table.GetDataWithContinuationTokenAsync(query, continuationToken);

            var itmes = new List<TxHistory>();
            foreach (var entity in data.Entities)
            {
                var history = entity.ToDomain();
                itmes.Add(history);
            }

            return (itmes, data.ContinuationToken);
        }

        public async Task<TxHistory> GetTopRecordAsync(TxDirectionType direction, string address)
        {
            var table = GetTable(address);

            var entity = await table.GetTopRecordAsync(GetPartitionKey(direction));
            if (entity != null)
            {
                var history = entity.ToDomain();
                return history;
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
            // hash index
            var index = new TxHistoryEntity
            {
                PartitionKey = GetPartitionKeyHashIndex(),
                RowKey = history.Hash,
                IndexedValue = history.InverseSequence.ToString()
            };
            await table.InsertOrReplaceAsync(index);
        }

        public async Task DeleteAsync(string address)
        {
            var table = GetTable(address);
            await table.DeleteAsync();
        }
    }
}
