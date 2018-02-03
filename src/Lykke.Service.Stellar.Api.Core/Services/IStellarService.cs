using System;
using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IStellarService
    {
        Boolean IsAddressValid(string address);

        Task<TxBroadcast> GetTxBroadcastAsync(Guid operationId);

        Task BroadcastTxAsync(Guid operationId, string xdrBase64);

        Task DeleteTxBroadcastAsync(Guid operationId);

        Task<Fees> GetFeesAsync();

        Task<long> GetAddressBalanceAsync(string address, Fees fees = null);

        Task<TxBuild> GetTxBuildAsync(Guid operationId);

        Task<string> BuildTransactionAsync(Guid operationId, string fromAddress, string toAddress, long amount);

        Task<Boolean> IsBalanceObservedAsync(string address);

        Task AddBalanceObservationAsync(string address);          Task DeleteBalanceObservationAsync(string address);          Task<WalletBalance[]> GetBalancesAsync(); 
    }
}
