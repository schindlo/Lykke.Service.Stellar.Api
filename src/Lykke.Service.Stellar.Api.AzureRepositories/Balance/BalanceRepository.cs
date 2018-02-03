using System;
using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Balance
{
    public class BalanceRepository: IBalanceRepository
    {
        private static string GetPartitionKey() => "";
        private static string GetRowKey() => Guid.NewGuid().ToString();

        private INoSQLTableStorage<BalanceEntity> _table;

        public BalanceRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _table = AzureTableStorage<BalanceEntity>.Create(dataConnStringManager, "Balance", log);
        }

        public async Task<WalletBalance[]> GetAsync()
        {
            var all = await _table.GetDataAsync();
            var balances = new WalletBalance[all.Count];
            int i = 0;
            foreach(BalanceEntity entity in all)
            {
                balances[i++] = new WalletBalance
                {
                    Address = entity.Address,
                    AssetId = entity.AssetId,
                    Balance = entity.Balance,
                    Block = entity.Block
                };
            }

            return balances;
        }

        public async Task<WalletBalance> GetAsync(string address)
        {
            WalletBalance balance = null;
            TableQuery<BalanceEntity> query = new TableQuery<BalanceEntity>().Where(TableQuery.GenerateFilterCondition("Address", QueryComparisons.Equal, address));
            await _table.ExecuteAsync(query, results =>
            {
                var e = results.GetEnumerator();
                if(e.MoveNext())
                {
                    var entity = e.Current;
                    balance = new WalletBalance
                    {
                        Address = entity.Address,
                        AssetId = entity.AssetId,
                        Balance = entity.Balance,
                        Block = entity.Block
                    };
                }
            });

            return balance;
        }

        public async Task AddAsync(string address)
        {
            var entity = new BalanceEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetRowKey(),
                Address = address
            };

            await _table.InsertAsync(entity);
        }

        public async Task DeleteAsync(string address)
        {
            TableQuery<BalanceEntity> query = new TableQuery<BalanceEntity>().Where(TableQuery.GenerateFilterCondition("Address", QueryComparisons.Equal, address));
            await _table.ExecuteAsync(query, results =>
            {
                var e = results.GetEnumerator();
                if (e.MoveNext())
                {
                    var entity = e.Current;
                    _table.DeleteAsync(GetPartitionKey(), entity.RowKey);
                }
            });
        }
    }
}
