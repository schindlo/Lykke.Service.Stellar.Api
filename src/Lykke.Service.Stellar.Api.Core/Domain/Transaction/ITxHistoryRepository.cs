using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public interface ITxHistoryRepository
    {
        Task<(List<TxHistory> Items, string ContinuationToken)> GetAllAsync(TxDirectionType direction, int take, string continuationToken);

        Task<List<TxHistory>> GetAllAfterHashAsync(TxDirectionType direction, string memo, int take, string afterKey);

        Task InsertOrReplaceAsync(TxDirectionType direction, TxHistory history);
    }
}
