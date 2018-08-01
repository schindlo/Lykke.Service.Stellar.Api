using System;
using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using JetBrains.Annotations;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public class TxBroadcastRepository : ITxBroadcastRepository
    {
        private const string TableName = "TransactionBroadcast";

        private readonly INoSQLTableStorage<TxBroadcastEntity> _table;

        [UsedImplicitly]
        public TxBroadcastRepository(IReloadingManager<string> dataConnStringManager,
                                     ILog log)
        {
            _table = AzureTableStorage<TxBroadcastEntity>.Create(dataConnStringManager, TableName, log);
        }

        public async Task<TxBroadcast> GetAsync(Guid operationId)
        {
            var rowKey = TableKeyHelper.GetRowKey(operationId);
            var entity = await _table.GetDataAsync(TableKeyHelper.GetHashedRowKey(rowKey), rowKey);
            var broadcast = entity?.ToDomain();
            return broadcast;
        }

        public async Task InsertOrReplaceAsync(TxBroadcast broadcast)
        {
            var entity = broadcast.ToEntity();
            await _table.InsertOrReplaceAsync(entity);
        }

        public async Task MergeAsync(TxBroadcast broadcast)
        {
            TxBroadcastEntity MergeAction(TxBroadcastEntity entity)
            {
                entity.State = broadcast.State;
                entity.Amount = broadcast.Amount;
                entity.Fee = broadcast.Fee;
                entity.Ledger = broadcast.Ledger;
                entity.CreatedAt = broadcast.CreatedAt;
                entity.Error = broadcast.Error;
                entity.ErrorCode = broadcast.ErrorCode;

                return entity;
            }

            var rowKey = TableKeyHelper.GetRowKey(broadcast.OperationId);
            await _table.MergeAsync(TableKeyHelper.GetHashedRowKey(rowKey), rowKey, MergeAction);
        }

        public async Task DeleteAsync(Guid operationId)
        {
            var rowKey = TableKeyHelper.GetRowKey(operationId);
            await _table.DeleteAsync(TableKeyHelper.GetHashedRowKey(rowKey), rowKey);
        }
    }
}
