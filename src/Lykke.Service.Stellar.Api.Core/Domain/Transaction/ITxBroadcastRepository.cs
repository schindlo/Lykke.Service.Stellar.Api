using System;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public interface ITxBroadcastRepository
    {
        Task<TxBroadcast> GetAsync(Guid operationId);

        Task<Guid?> GetOperationId(string hash);

        Task InsertOrReplaceAsync(TxBroadcast broadcast);

        Task MergeAsync(TxBroadcast broadcast);

        Task DeleteAsync(Guid operationId);
    }
}
