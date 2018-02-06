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
        private static string GetPartitionKey() => "Broadcast";
        private static string GetRowKey(Guid operationId) => operationId.ToString();

        private INoSQLTableStorage<TxBroadcastEntity> _table;

        public TxBroadcastRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _table = AzureTableStorage<TxBroadcastEntity>.Create(dataConnStringManager, "StellarApiTransaction", log);
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
                    Amount = entity.Amount,
                    Fee = entity.Fee,
                    Hash = entity.Hash,
                    Ledger = entity.Ledger,
                    CreatedAt = entity.CreatedAt,
                    Error = entity.Error,
                    ErrorCode = entity.ErrorCode
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
                Amount = broadcast.Amount,
                Fee = broadcast.Fee,
                Hash = broadcast.Hash,
                Ledger = broadcast.Ledger,
                CreatedAt = broadcast.CreatedAt,
                Error = broadcast.Error,
                ErrorCode = broadcast.ErrorCode
            };

            await _table.InsertAsync(entity);
        }

        public async Task DeleteAsync(Guid operationId)
        {
            await _table.DeleteAsync(GetPartitionKey(), GetRowKey(operationId));
        }
    }
}
