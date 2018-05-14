using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Balance
{
    public class WalletBalanceRepository : IWalletBalanceRepository
    {
        private const string TableName = "WalletBalance";

        private readonly INoSQLTableStorage<WalletBalanceEntity> _table;

        public WalletBalanceRepository(IReloadingManager<string> dataConnStringManager,
                                       ILog log)
        {
            _table = AzureTableStorage<WalletBalanceEntity>.Create(dataConnStringManager, TableName, log);
        }

        public async Task<(List<WalletBalance> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken)
        {
            var filter = TableQuery.GenerateFilterConditionForLong(nameof(WalletBalanceEntity.Balance), QueryComparisons.GreaterThan, 0);
            var query = new TableQuery<WalletBalanceEntity>().Where(filter).Take(take);
            var data = await _table.GetDataWithContinuationTokenAsync(query, continuationToken);

            var balances = data.Entities.Select(x => x.ToDomain()).ToList();
            return (balances, data.ContinuationToken);
        }

        public async Task<WalletBalance> GetAsync(string assetId, string address)
        {
            var rowKey = WalletBalanceEntity.GetRowKey(assetId, address);
            var entity = await _table.GetDataAsync(TableKey.GetHashedRowKey(rowKey), rowKey);
            if (entity != null)
            {
                var wallet = entity.ToDomain();
                return wallet;
            }

            return null;
        }

        public async Task InsertOrReplaceAsync(WalletBalance balance)
        {
            var entity = balance.ToEntity();
            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task DeleteIfExistAsync(string assetId, string address)
        {
            var rowKey = WalletBalanceEntity.GetRowKey(assetId, address);
            await _table.DeleteIfExistAsync(TableKey.GetHashedRowKey(rowKey), rowKey);
        }

        public async Task<bool> IncreaseBalanceAsync(string assetId, string address, long ledger, int operationIndex, string hash, long amount)
        {
            var rowKey = WalletBalanceEntity.GetRowKey(assetId, address);
            var partitionKey = TableKey.GetHashedRowKey(rowKey);

            WalletBalanceEntity CreateEntity()
            {
                return new WalletBalanceEntity
                {
                    PartitionKey = partitionKey,
                    RowKey = rowKey,
                    Balance = amount,
                    Ledger = ledger,
                    OperationIndex = operationIndex,
                    LastTransactionHash = hash
                };
            }

            // ReSharper disable once ImplicitlyCapturedClosure
            bool ModifyEntity(WalletBalanceEntity entity)
            {
                if (ledger > entity.Ledger ||
                    (ledger == entity.Ledger && operationIndex > entity.OperationIndex))
                {
                    entity.Balance += amount;
                    entity.Ledger = ledger;
                    entity.OperationIndex = operationIndex;
                    entity.LastTransactionHash = hash;
                    return true;
                }

                return false;
            }

            var result = await _table.InsertOrModifyAsync(partitionKey, rowKey, CreateEntity, ModifyEntity);
            return result;
        }

        public async Task<bool> DecreaseBalanceAsync(string assetId, string address, string hash, long amount)
        {
            var rowKey = WalletBalanceEntity.GetRowKey(assetId, address);
            var partitionKey = TableKey.GetHashedRowKey(rowKey);

            // ReSharper disable once ImplicitlyCapturedClosure
            WalletBalanceEntity ModifyEntity(WalletBalanceEntity entity)
            {
                if (!hash.Equals(entity.LastTransactionHash, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (entity.Balance < amount)
                    {
                        return null;
                    }

                    entity.Balance -= amount;
                    entity.LastTransactionHash = hash;
                }

                return entity;
            }

            var result = await _table.MergeAsync(partitionKey, rowKey, ModifyEntity);
            return result != null;
        }

        public async Task<bool> DeleteIfBalanceIsZero(string assetId, string address)
        {
            var rowKey = WalletBalanceEntity.GetRowKey(assetId, address);
            var partitionKey = TableKey.GetHashedRowKey(rowKey);

            var entity = await _table.GetDataAsync(partitionKey, rowKey);
            if (entity != null && entity.Balance == 0)
            {
                await _table.DeleteAsync(entity);
                return true;
            }

            return false;
        }
    }
}
