using System;
using System.Threading.Tasks;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public class TxBuildRepository: ITxBuildRepository
    {
        private static string GetPartitionKey() => "";
        private static string GetRowKey(Guid operationId) => operationId.ToString();

        private INoSQLTableStorage<TxBuildEntity> _table;

        public TxBuildRepository(IReloadingManager<string> dataConnStringManager, ILog log)
        {
            _table = AzureTableStorage<TxBuildEntity>.Create(dataConnStringManager, "TxBuilds", log);
        }

        public async Task<TxBuild> GetAsync(Guid operationId)
        {
            var entity = await _table.GetDataAsync(GetPartitionKey(), GetRowKey(operationId));
            if (entity != null)
            {
                return new TxBuild
                {
                    OperationId = entity.OperationId,
                    Timestamp = entity.Timestamp,
                    XdrBase64 = entity.XdrBase64
                };
            }

            return null;
        }

        public async Task AddAsync(TxBuild build)
        {
            var entity = new TxBuildEntity
            {
                PartitionKey = GetPartitionKey(),
                RowKey = GetRowKey(build.OperationId),
                Timestamp = build.Timestamp,
                XdrBase64 = build.XdrBase64
            };

            await _table.InsertAsync(entity);
        }

        public async Task DeleteAsync(Guid operationId)
        {
            await _table.DeleteAsync(GetPartitionKey(), GetRowKey(operationId));
        }
    }
}
