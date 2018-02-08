using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Lykke.AzureStorage.Tables;
using System;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    public class ObservationRepository<T, U> : IObservationRepository<U> where T : ObservationEntity<U>, new() where U : class
    {
        private INoSQLTableStorage<T> _table;

        public ObservationRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _table = AzureTableStorage<T>.Create(dataConnStringManager, "Observation", log);
        }

        public async Task<(List<U> Items, string ContinuationToken)> GetAllAsync(int take, string continuationToken)
        {
            var query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, typeof(U).Name))
                                           .Take(take);
            var data = await _table.GetDataWithContinuationTokenAsync(query, continuationToken);

            var observations = new List<U>();
            foreach (var entity in data.Entities)
            {
                var observation = entity.ToDomain();
                observations.Add(observation);
            }

            return (observations, data.ContinuationToken);
        }

        public async Task<U> GetAsync(string key)
        {
            var entity = await _table.GetDataAsync(typeof(U).Name, key);
            if (entity != null)
            {
                var result = entity.ToDomain();
                return result;
            }

            return null;
        }

        public async Task InsertOrReplaceAsync(U observation)
        {
            var entity = new T()
            {
                PartitionKey = typeof(U).Name,
                Timestamp = DateTimeOffset.UtcNow,
            };
            entity.ToEntity(observation);
            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task DeleteIfExistAsync(string key)
        {
            await _table.DeleteIfExistAsync(typeof(U).Name, key);
        }
    }
}
