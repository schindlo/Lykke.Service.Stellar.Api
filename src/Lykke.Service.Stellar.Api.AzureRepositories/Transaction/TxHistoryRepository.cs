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
        private static string GetPartitionKey() => "Transaction";
        private static string GetRowKey(ulong paymentOperationId) => (UInt64.MaxValue - paymentOperationId).ToString();

        private IReloadingManager<string> _dataConnStringManager;

        private ILog _log;

        public TxHistoryRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _dataConnStringManager = dataConnStringManager;
            _log = log;
        }

        private INoSQLTableStorage<TxHistoryEntity> GetTable(TxAddressType type, string address)
        {
            // TODO: cache
            var tableName = $"{type}{address}";
            var table = AzureTableStorage<TxHistoryEntity>.Create(_dataConnStringManager, tableName, _log);
            return table;
        }

        public async Task<(List<TxHistory> Items, string ContinuationToken)> GetAllAsync(TxAddressType type, string address, int take, string continuationToken)
        {
            var table = GetTable(type, address);
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, address);
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

        public async Task<TxHistory> GetTopRecordAsync(TxAddressType type, string address)
        {
            var table = GetTable(type, address);
            var entity = await table.GetTopRecordAsync(GetPartitionKey());
            var history = entity.ToDomain();
            return history;
        }

        public async Task AddAsync(TxAddressType type, TxHistory history)
        {
            var address = type == TxAddressType.From ? history.FromAddress : history.ToAddress;
            var table = GetTable(type, address);

            var entity = new TxHistoryEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetRowKey(history.PaymentOperationId),
                Timestamp = history.Timestamp,
                FromAddress = history.FromAddress,
                ToAddress = history.ToAddress,
                AssetId = history.AssetId,
                Amount = history.Amount,
                Hash = history.Hash,
                PaymentOperationId = history.PaymentOperationId
            };

            await table.InsertAsync(entity);
        }

        public async Task DeleteAsync(TxAddressType type, string address)
        {
            var table = GetTable(type, address);
            await table.DeleteAsync();
        }
    }
}
