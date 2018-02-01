using System;
using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public class TxBroadcastRepository: ITxBroadcastRepository
    {
        private static string GetPartitionKey() => "";
        private static string GetRowKey(Guid operationId) => operationId.ToString();

        private INoSQLTableStorage<TxBroadcastEntity> _table;

        public TxBroadcastRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _table = AzureTableStorage<TxBroadcastEntity>.Create(dataConnStringManager, "TxBroadcasts", log);
        }

        public async Task<TxBroadcast> GetAsync(Guid operationId)
        {
            var entity = await _table.GetDataAsync(GetPartitionKey(), GetRowKey(operationId));
            if (entity != null)
            {
                return new TxBroadcast
                {
                    OperationId = entity.OperationId,
                    State = entity.State,
                    Hash = entity.Hash
                };
            }

            return null;
        }

        public async Task AddAsync(TxBroadcast broadcast)
        {
            var entity = new TxBroadcastEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetRowKey(broadcast.OperationId),
                State = broadcast.State,
                Hash = broadcast.Hash
            };

            await _table.InsertAsync(entity);
        }

        public async Task DeleteAsync(Guid operationId)
        {
            await _table.DeleteAsync(GetPartitionKey(), GetRowKey(operationId));
        }
    }
}
