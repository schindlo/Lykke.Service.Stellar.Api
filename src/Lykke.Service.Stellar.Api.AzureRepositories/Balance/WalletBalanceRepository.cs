using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Balance
{
    public class WalletBalanceRepository : IWalletBalanceRepository
    {
        private const string TableName = "WalletBalance";

        private static string GetPartitionKey() => Asset.Stellar.Id;

        private static string GetAddressRowKey(string address) => address;
        private static string GetCurrentRowKey() => "Current";

        private INoSQLTableStorage<WalletBalanceEntity> _table;
        private INoSQLTableStorage<IndexEntity> _tableIndex;

        public WalletBalanceRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _table = AzureTableStorage<WalletBalanceEntity>.Create(dataConnStringManager, TableName, log);
            _tableIndex = AzureTableStorage<IndexEntity>.Create(dataConnStringManager, TableName, log);
        }

        public async Task<(List<WalletBalance> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken)
        {
            var filter = TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.Equal, GetPartitionKey());
            var query = new TableQuery<WalletBalanceEntity>().Where(filter).Take(take);
            var data = await _table.GetDataWithContinuationTokenAsync(query, continuationToken);

            var balances = new List<WalletBalance>();
            foreach (var entity in data.Entities)
            {
                var balance = entity.ToDomain();
                balances.Add(balance);
            }

            return (balances, data.ContinuationToken);
        }

        public async Task<WalletBalance> GetAsync(string address)
        {
            var entity = await _table.GetDataAsync(GetPartitionKey(), GetAddressRowKey(address));
            if (entity != null)
            {
                var wallet = entity.ToDomain();
                return wallet;
            }

            return null;
        }

        public async Task InsertOrReplaceAsync(WalletBalance balance)
        {
            var entity = new WalletBalanceEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetAddressRowKey(balance.Address),
                Balance = balance.Balance,
                Ledger = balance.Ledger,
                OperationCount = balance.OperationCount
            };

            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task DeleteIfExistAsync(string address)
        {
            await _table.DeleteIfExistAsync(GetPartitionKey(), GetAddressRowKey(address));
        }

        public async Task<string> GetCurrentPagingToken()
        {
            var pagingTokenIndex = await _tableIndex.GetDataAsync(IndexEntity.GetPartitionKeyPagingToken(), GetCurrentRowKey());
            if (pagingTokenIndex != null)
            {
                return pagingTokenIndex.Value;
            }

            return null;
        }

        public async Task SetCurrentPagingToken(string pagingToken)
        {
            // index to current paging token
            var entity = new IndexEntity
            {
                PartitionKey = IndexEntity.GetPartitionKeyPagingToken(),
                RowKey = GetCurrentRowKey(),
                Value = pagingToken
            };
            await _tableIndex.InsertOrReplaceAsync(entity);
        }
    }
}
