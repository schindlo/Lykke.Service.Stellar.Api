using System;
using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface ITransactionService
    {
        Task<TxBroadcast> GetTxBroadcastAsync(Guid operationId);

        Task BroadcastTxAsync(Guid operationId, string xdrBase64, TxBroadcast broadcast = null);

        Task DeleteTxBroadcastAsync(Guid operationId);

        Task<Fees> GetFeesAsync();

        Task<TxBuild> GetTxBuildAsync(Guid operationId);

        Task<string> BuildTransactionAsync(Guid operationId, AddressBalance from, string toAddress, string memoText, long amount);

        Task<int> UpdateBroadcastsInProgress(int batchSize);

        bool CheckSignature(string xdrBase64);

        string GetLastJobError();
    }
}
