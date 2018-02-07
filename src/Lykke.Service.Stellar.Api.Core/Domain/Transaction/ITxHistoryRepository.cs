using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public interface ITxHistoryRepository
    {
        Task<(List<TxHistory> Items, string ContinuationToken)> GetAllAsync(TxDirectionType direction, string address, int take, string continuationToken);

        Task<List<TxHistory>> GetAllAfterHashAsync(TxDirectionType direction, string address, int take, string afterHash);

        Task<TxHistory> GetLastRecordAsync(string address);

        Task InsertOrReplaceAsync(TxDirectionType direction, TxHistory history);

        Task DeleteAsync(string address);
    }
}
