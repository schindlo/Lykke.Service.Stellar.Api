using System;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Balance
{
    public interface IBalanceRepository
    {
        Task<WalletBalance> GetAsync(string address);
        Task<WalletBalance[]> GetAsync();
        Task AddAsync(string address);
        Task DeleteAsync(string address);
    }
}
