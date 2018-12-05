using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Balance
{
    public class WalletBalanceRepository : IWalletBalanceRepository
    {
        private const string BalanceTableName = "WalletBalance";
        private const string JournalTableName = "WalletBalanceJournal";

        private readonly INoSQLTableStorage<WalletBalanceEntity> _balanceTable;
        private readonly INoSQLTableStorage<WalletBalanceJournalEntity> _journalTable;


        [UsedImplicitly]
        public WalletBalanceRepository(IReloadingManager<string> dataConnStringManager,
                                       ILogFactory logFactory)
        {
            _balanceTable = AzureTableStorage<WalletBalanceEntity>.Create(dataConnStringManager, BalanceTableName, logFactory);
            _journalTable = AzureTableStorage<WalletBalanceJournalEntity>.Create(dataConnStringManager, JournalTableName, logFactory);
        }

        public async Task<(List<WalletBalance> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken)
        {
            var data = await _balanceTable.GetDataWithContinuationTokenAsync(take, continuationToken);
            var balances = data.Entities.Select(x => x.ToDomain()).ToList();
            return (balances, data.ContinuationToken);
        }

        public async Task<WalletBalance> GetAsync(string assetId, string address)
        {
            var rowKey = WalletBalanceEntity.GetRowKey(assetId, address);
            var entity = await _balanceTable.GetDataAsync(TableKeyHelper.GetHashedRowKey(rowKey), rowKey);
            var wallet = entity?.ToDomain();
            return wallet;
        }

        public async Task DeleteIfExistAsync(string assetId, string address)
        {
            var rowKey = WalletBalanceEntity.GetRowKey(assetId, address);
            await _balanceTable.DeleteIfExistAsync(TableKeyHelper.GetHashedRowKey(rowKey), rowKey);
        }

        public async Task RecordOperationAsync(string assetId, string address, long ledger, long operationId, string transactionHash, long amount)
        {
            await _journalTable.InsertOrReplaceAsync(new WalletBalanceJournalEntity
            {
                PartitionKey = WalletBalanceJournalEntity.GetPartitionKey(assetId, address),
                RowKey = WalletBalanceJournalEntity.GetRowKey(transactionHash, operationId),
                AssetId = assetId,
                Address = address,
                Ledger = ledger,
                TransactionHash = transactionHash,
                OperationId = operationId,
                Amount = amount
            });
        }

        public async Task RefreshBalance(IEnumerable<(string assetId, string address)> wallets)
        {
            if (wallets.Any())
            {
                using (var semaphore = new SemaphoreSlim(10))
                {
                    var tasks = wallets.Select(async x =>
                    {
                        try
                        {
                            await semaphore.WaitAsync();
                            await RefreshBalance(x.assetId, x.address);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(tasks);
                }
            }
        }

        public async Task RefreshBalance(string assetId, string address)
        {
            var balanceRowKey = WalletBalanceEntity.GetRowKey(assetId, address);
            var balancePartitionKey = TableKeyHelper.GetHashedRowKey(balanceRowKey);
            var records = await _journalTable.GetDataAsync(WalletBalanceJournalEntity.GetPartitionKey(assetId, address));
            var balance = records.Aggregate(
                new WalletBalanceEntity { PartitionKey = balancePartitionKey, RowKey = balanceRowKey },
                (b, j) =>
                {
                    b.Balance += j.Amount;
                    b.Ledger = Math.Max(b.Ledger, j.Ledger);
                    return b;
                }
            );

            if (balance.Balance == 0)
                await _balanceTable.DeleteIfExistAsync(balancePartitionKey, balanceRowKey);
            else
                await _balanceTable.InsertOrMergeAsync(balance);
        }
    }
}
