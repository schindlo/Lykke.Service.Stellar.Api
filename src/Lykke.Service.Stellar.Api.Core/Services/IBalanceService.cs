using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IBalanceService
    {
        bool IsAddressValid(string address);

        Task<AddressBalance> GetAddressBalanceAsync(string address, Fees fees = null);

        Task<bool> IsBalanceObservedAsync(string address);

        Task AddBalanceObservationAsync(string address);

        Task DeleteBalanceObservationAsync(string address);

        Task<(List<WalletBalance> Wallets, string ContinuationToken)> GetBalancesAsync(int take, string continuationToken);

        Task<int> UpdateWalletBalances();

        string GetLastJobError();
    }
}
