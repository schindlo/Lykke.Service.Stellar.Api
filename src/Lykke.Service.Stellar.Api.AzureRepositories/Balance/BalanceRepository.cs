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
    public class BalanceRepository: IBalanceRepository
    {
        private static string GetPartitionKey(string address) => Asset.Stellar.Id + ":" + address;
        private static string GetRowKey(string destinationTag) => destinationTag ?? string.Empty;

        private INoSQLTableStorage<BalanceEntity> _table;

        public BalanceRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _table = AzureTableStorage<BalanceEntity>.Create(dataConnStringManager, "StellarApiBalance", log);
        }

        public async Task<List<WalletBalance>> GetAllAsync()
        {
            var all = await _table.GetDataAsync();

            var balances = new List<WalletBalance>();
            foreach(BalanceEntity entity in all)
            {
                var balance = entity.ToDomain();
                balances.Add(balance);
            }

            return balances;
        }

        public async Task<(List<WalletBalance> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken)
        {
            var query = new TableQuery<BalanceEntity>().Take(take);
            var data = await _table.GetDataWithContinuationTokenAsync(query, continuationToken);

            var balances = new List<WalletBalance>();
            foreach (BalanceEntity entity in data.Entities)
            {
                var balance = entity.ToDomain();
                balances.Add(balance);
            }

            return (balances, data.ContinuationToken);
        }

        public async Task<WalletBalance> GetAsync(string address, string destinationTag)
        {
            var entity = await _table.GetDataAsync(GetPartitionKey(address), GetRowKey(destinationTag));
            if (entity != null)
            {
                var balance = entity.ToDomain();
                return balance;
            }

            return null;
        }

        public async Task AddAsync(string address, string destinationTag)
        {
            var entity = new BalanceEntity
            {
                PartitionKey = GetPartitionKey(address),
                RowKey = GetRowKey(destinationTag),
                Timestamp = DateTimeOffset.UtcNow
            };

            await _table.InsertAsync(entity);
        }

        public async Task DeleteAsync(string address, string destinationTag)
        {
            await _table.DeleteAsync(GetPartitionKey(address), GetRowKey(destinationTag));
        }
    }
}
