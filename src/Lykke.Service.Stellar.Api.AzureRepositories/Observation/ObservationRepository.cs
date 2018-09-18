﻿using System;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage.Table;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Lykke.Common.Log;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Observation
{
    public class ObservationRepository<T, TU> : IObservationRepository<TU> where T : ObservationEntity<TU>, new() where TU : class
    {
        private readonly INoSQLTableStorage<T> _table;

        [UsedImplicitly]
        public ObservationRepository(string tableName,
                                     IReloadingManager<string> dataConnStringManager,
                                     ILogFactory logFactory)
        {
            _table = AzureTableStorage<T>.Create(dataConnStringManager, tableName, logFactory);
        }

        public async Task<(List<TU> Items, string ContinuationToken)> GetAllAsync(int take, string continuationToken)
        {
            var query = new TableQuery<T>().Take(take);
            var data = await _table.GetDataWithContinuationTokenAsync(query, continuationToken);

            var observations = data.Entities.Select(x => x.ToDomain()).ToList();
            return (observations, data.ContinuationToken);
        }

        public async Task<TU> GetAsync(string key)
        {
            var entity = await _table.GetDataAsync(TableKeyHelper.GetHashedRowKey(key), key);
            var result = entity?.ToDomain();
            return result;
        }

        public async Task InsertOrReplaceAsync(TU observation)
        {
            var entity = new T
            {
                Timestamp = DateTimeOffset.UtcNow
            };
            entity.ToEntity(observation);
            entity.PartitionKey = TableKeyHelper.GetHashedRowKey(entity.RowKey);
            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task DeleteIfExistAsync(string key)
        {
            await _table.DeleteIfExistAsync(TableKeyHelper.GetHashedRowKey(key), key);
        }
    }
}
