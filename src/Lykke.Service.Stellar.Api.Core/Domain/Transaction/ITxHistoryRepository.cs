using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public interface ITxHistoryRepository
    {
        Task<(List<TxHistory> Items, string ContinuationToken)> GetAllAsync(TxAddressType type, string address, int take, string continuationToken);

        Task<TxHistory> GetTopRecordAsync(TxAddressType type, string address);

        Task AddAsync(TxAddressType type, TxHistory history);

        Task DeleteAsync(TxAddressType type, string address);
    }
}
