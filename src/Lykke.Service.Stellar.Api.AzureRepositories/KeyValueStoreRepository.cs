using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using JetBrains.Annotations;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Core.Domain;

namespace Lykke.Service.Stellar.Api.AzureRepositories
{
    public class KeyValueStoreRepository : IKeyValueStoreRepository
    {
        private const string TableName = "KeyValueStore";

        private readonly INoSQLTableStorage<KeyValueEntity> _table;

        [UsedImplicitly]
        public KeyValueStoreRepository(IReloadingManager<string> dataConnStringManager,
                                       ILog log)
        {
            _table = AzureTableStorage<KeyValueEntity>.Create(dataConnStringManager, TableName, log);
        }

        public async Task<string> GetAsync(string key)
        {
            var entity = await _table.GetDataAsync(string.Empty, key);
            return entity?.Value;
        }

        public async Task SetAsync(string key, string value)
        {
            var entity = new KeyValueEntity
            {
                PartitionKey = string.Empty,
                RowKey = key,
                Value = value
            };
            await _table.InsertOrReplaceAsync(entity);
        }
    }
}
