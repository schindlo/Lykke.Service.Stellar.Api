using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Common.Log;
using AzureStorage;
using AzureStorage.Tables;
using JetBrains.Annotations;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public class TxHistoryRepository : ITxHistoryRepository
    {
        private const string TableNamePrefix = "TransactionHistoryDepositBase";

        private readonly INoSQLTableStorage<TxHistoryEntity> _tableIn;
        private readonly INoSQLTableStorage<TxHistoryEntity> _tableOut;

        [UsedImplicitly]
        public TxHistoryRepository(IReloadingManager<string> dataConnStringManager,
                                   ILog log)
        {
            _tableIn = AzureTableStorage<TxHistoryEntity>.Create(dataConnStringManager, $"{TableNamePrefix}In", log);
            _tableOut = AzureTableStorage<TxHistoryEntity>.Create(dataConnStringManager, $"{TableNamePrefix}Out", log);
        }

        private INoSQLTableStorage<TxHistoryEntity> GetTable(TxDirectionType direction)
        {
            return direction == TxDirectionType.Incoming ? _tableIn : _tableOut;
        }

        public async Task<List<TxHistory>> GetAllAfterHashAsync(TxDirectionType direction, string memo, int take, string afterKey)
        {
            // memo on transaction is assigned to destination
            if (direction == TxDirectionType.Outgoing && !string.IsNullOrEmpty(memo))
            {
                return new List<TxHistory>();
            }

            var table = GetTable(direction);

            // build filter
            string filter = null;
            if (!string.IsNullOrEmpty(afterKey))
            {
                filter = TableQuery.GenerateFilterCondition(nameof(ITableEntity.PartitionKey), QueryComparisons.GreaterThan, afterKey);
            }
            if (!string.IsNullOrEmpty(memo))
            {
                var rkFilter = TableQuery.GenerateFilterCondition(nameof(ITableEntity.RowKey), QueryComparisons.Equal, memo);
                filter = filter != null ? TableQuery.CombineFilters(filter, TableOperators.And, rkFilter) : rkFilter;
            }

            var query = new TableQuery<TxHistoryEntity>();
            if (filter != null)
            {
                query = query.Where(filter);
            }
            query = query.Take(take);
            var data = await table.GetDataWithContinuationTokenAsync(query, null);
            var items = data.Entities.ToDomain();
            return items;
        }

        public async Task InsertOrReplaceAsync(TxDirectionType direction, TxHistory history)
        {
            var table = GetTable(direction);

            // history entry
            var entity = history.ToEntity();
            await table.InsertOrReplaceAsync(entity);
        }
    }
}
