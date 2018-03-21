using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public interface ITxHistoryRepository
    {
        Task<(List<TxHistory> Items, string ContinuationToken)> GetAllAsync(string tableId, TxDirectionType direction, int take, string continuationToken);

        Task<List<TxHistory>> GetAllAfterHashAsync(string tableId, TxDirectionType direction, int take, string afterHash);

        Task<string> GetCurrentPagingToken(string tableId);

        Task SetCurrentPagingToken(string tableId, string pagingToken);

        Task InsertOrReplaceAsync(string tableId, TxDirectionType direction, TxHistory history);

        Task DeleteAsync(string tableId);
    }
}
