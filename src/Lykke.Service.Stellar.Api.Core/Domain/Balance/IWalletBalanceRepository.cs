using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Balance
{
    public interface IWalletBalanceRepository
    {
        Task<(List<WalletBalance> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken);

        Task<WalletBalance> GetAsync(string address);

        Task InsertOrReplaceAsync(WalletBalance balance);

        Task DeleteIfExistAsync(string address);
    }
}
