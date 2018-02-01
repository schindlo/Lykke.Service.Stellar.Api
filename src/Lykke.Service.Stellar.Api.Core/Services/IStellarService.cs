using System;
using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IStellarService
    {
        Task<TxBroadcast> GetTxBroadcastAsync(Guid operationId);

        Task BroadcastTxAsync(Guid operationId, string xdrBase64);
    }
}
