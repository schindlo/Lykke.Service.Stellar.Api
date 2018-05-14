using System;
using System.Linq;
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
    public class ObservationRepository<T, U> : IObservationRepository<U> where T : ObservationEntity<U>, new() where U : class
    {
        private readonly INoSQLTableStorage<T> _table;

        public ObservationRepository(string tableName, IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _table = AzureTableStorage<T>.Create(dataConnStringManager, tableName, log);
        }

        public async Task<(List<U> Items, string ContinuationToken)> GetAllAsync(int take, string continuationToken)
        {
            var query = new TableQuery<T>().Take(take);
            var data = await _table.GetDataWithContinuationTokenAsync(query, continuationToken);

            var observations = data.Entities.Select(x => x.ToDomain()).ToList();
            return (observations, data.ContinuationToken);
        }

        public async Task<U> GetAsync(string key)
        {
            var entity = await _table.GetDataAsync(TableKey.GetHashedRowKey(key), key);
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
                Timestamp = DateTimeOffset.UtcNow,
            };
            entity.ToEntity(observation);
            entity.PartitionKey = TableKey.GetHashedRowKey(entity.RowKey);
            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task DeleteIfExistAsync(string key)
        {
            await _table.DeleteIfExistAsync(TableKey.GetHashedRowKey(key), key);
        }
    }
}
