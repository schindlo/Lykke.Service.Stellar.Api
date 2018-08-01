using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Balance
{
    public interface IWalletBalanceRepository
    {
        Task<(List<WalletBalance> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken);

        Task<WalletBalance> GetAsync(string assetId, string address);

        Task DeleteIfExistAsync(string assetId, string address);

        Task<bool> IncreaseBalanceAsync(string assetId, string address, long ledger, int operationIndex, string hash, long amount);

        Task<bool> DecreaseBalanceAsync(string assetId, string address, string hash, long amount);

        Task<bool> DeleteIfBalanceIsZero(string assetId, string address);
    }
}
