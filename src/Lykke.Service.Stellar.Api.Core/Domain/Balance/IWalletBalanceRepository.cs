using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Balance
{
    public interface IWalletBalanceRepository
    {
        Task<(List<WalletBalance> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken);

        Task<WalletBalance> GetAsync(string assetId, string address);

        Task DeleteIfExistAsync(string assetId, string address);

        Task RecordOperationAsync(string assetId, string address, long ledger, long operationId, string transactionHash, long amount);

        Task RefreshBalance(IEnumerable<(string assetId, string address)> wallets);

        Task RefreshBalance(string assetId, string address);
    }
}
