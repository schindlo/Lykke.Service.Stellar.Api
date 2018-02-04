using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Balance
{
    public interface IBalanceRepository
    {
        Task<List<WalletBalance>> GetAllAsync();

        Task<(List<WalletBalance> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken);

        Task<WalletBalance> GetAsync(string address, string destinationTag);

        Task AddAsync(string address, string destinationTag);

        Task DeleteAsync(string address, string destinationTag);
    }
}
