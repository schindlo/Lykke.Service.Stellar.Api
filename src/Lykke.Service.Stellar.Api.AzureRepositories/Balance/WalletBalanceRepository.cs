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
    public class WalletBalanceRepository: IWalletBalanceRepository
    {
        private static string GetPartitionKey() => Asset.Stellar.Id;
        private static string GetRowKey(string address) => address;

        private INoSQLTableStorage<WalletBalanceEntity> _table;

        public WalletBalanceRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _table = AzureTableStorage<WalletBalanceEntity>.Create(dataConnStringManager, "StellarApiWalletBalance", log);
        }

        public async Task<(List<WalletBalance> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken)
        {
            var query = new TableQuery<WalletBalanceEntity>().Take(take);
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
            var entity = await _table.GetDataAsync(GetPartitionKey(), GetRowKey(address));
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
                RowKey = GetRowKey(balance.Address),
                Timestamp = DateTimeOffset.UtcNow,
                Balance = balance.Balance,
                Ledger = balance.Ledger
            };

            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task DeleteIfExistAsync(string address)
        {
            await _table.DeleteIfExistAsync(GetPartitionKey(), GetRowKey(address));
        }
    }
}
