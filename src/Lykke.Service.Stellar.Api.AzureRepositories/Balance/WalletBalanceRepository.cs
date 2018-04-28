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

        public WalletBalanceRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _table = AzureTableStorage<WalletBalanceEntity>.Create(dataConnStringManager, TableName, log);
        }

        public async Task<(List<WalletBalance> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken)
        {
            var query = new TableQuery<WalletBalanceEntity>().Take(take);
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
    }
}
