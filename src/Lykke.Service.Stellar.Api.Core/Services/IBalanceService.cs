using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IBalanceService
    {
        bool IsAddressValid(string address, out bool hasExtension);

        string GetDepositBaseAddress();

        string GetBaseAddress(string address);

        string GetPublicAddressExtension(string address);

        Task<AddressBalance> GetAddressBalanceAsync(string address, Fees fees = null);

        Task<bool> DecreaseBalance(string address, long amount);
       
        Task<bool> IsBalanceObservedAsync(string address);

        Task AddBalanceObservationAsync(string address);

        Task DeleteBalanceObservationAsync(string address);

        Task<(List<WalletBalance> Wallets, string ContinuationToken)> GetBalancesAsync(int take, string continuationToken);

        Task<int> UpdateWalletBalances();

        string GetLastJobError();
    }
}
