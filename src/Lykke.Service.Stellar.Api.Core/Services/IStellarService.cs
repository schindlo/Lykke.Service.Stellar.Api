using System;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IStellarService
    {
        Task BroadcastAsync(Guid operationId, string xdrBase64);
    }
}
