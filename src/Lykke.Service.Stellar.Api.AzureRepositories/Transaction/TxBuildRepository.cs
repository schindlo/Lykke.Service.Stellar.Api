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
    public class TxBuildRepository : ITxBuildRepository
    {
        private const string TableName = "TransactionBuild";

        private static string GetRowKey(Guid operationId) => operationId.ToString();

        private readonly INoSQLTableStorage<TxBuildEntity> _table;

        [UsedImplicitly]
        public TxBuildRepository(IReloadingManager<string> dataConnStringManager,
                                 ILog log)
        {
            _table = AzureTableStorage<TxBuildEntity>.Create(dataConnStringManager, TableName, log);
        }

        public async Task<TxBuild> GetAsync(Guid operationId)
        {
            var rowKey = GetRowKey(operationId);
            var entity = await _table.GetDataAsync(TableKeyHelper.GetHashedRowKey(rowKey), rowKey);
            var build = entity?.ToDomain();
            return build;
        }

        public async Task AddAsync(TxBuild build)
        {
            var entity = build.ToEntity();
            await _table.InsertAsync(entity);
        }
    }
}
