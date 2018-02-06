using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public interface ITxHistoryRepository
    {
        Task<(List<TxHistory> Items, string ContinuationToken)> GetAllAsync(TxDirectionType direction, string address, int take, string continuationToken);

        Task<TxHistory> GetTopRecordAsync(TxDirectionType direction, string address);

        Task InsertOrReplaceAsync(TxDirectionType direction, TxHistory history);

        Task DeleteAsync(string address);
    }
}
