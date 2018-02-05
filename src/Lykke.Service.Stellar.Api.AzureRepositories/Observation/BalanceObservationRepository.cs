using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    public class BalanceObservationRepository : IBalanceObservationRepository
    {
        private static string GetPartitionKey() => "BalanceObservation";
        private static string GetRowKey(string address) => address;

        private INoSQLTableStorage<ObservationEntity> _table;

        public BalanceObservationRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _table = AzureTableStorage<ObservationEntity>.Create(dataConnStringManager, "StellarApiObservation", log);
        }

        public async Task<(List<BalanceObservation> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken)
        {
            var query = new TableQuery<ObservationEntity>().Take(take);
            var data = await _table.GetDataWithContinuationTokenAsync(query, continuationToken);

            var observations = new List<BalanceObservation>();
            foreach (var entity in data.Entities)
            {
                var observation = entity.ToDomain();
                observations.Add(observation);
            }

            return (observations, data.ContinuationToken);
        }

        public async Task<BalanceObservation> GetAsync(string address)
        {
            var entity = await _table.GetDataAsync(GetPartitionKey(), GetRowKey(address));
            if (entity != null)
            {
                var observation = entity.ToDomain();
                return observation;
            }

            return null;
        }

        public async Task AddAsync(string address)
        {
            var entity = new ObservationEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetRowKey(address)
            };

            await _table.InsertAsync(entity);
        }

        public async Task DeleteAsync(string address)
        {
            await _table.DeleteAsync(GetPartitionKey(), GetRowKey(address));
        }
    }
}
