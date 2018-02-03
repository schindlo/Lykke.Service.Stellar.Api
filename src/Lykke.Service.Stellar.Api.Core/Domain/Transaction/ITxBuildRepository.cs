using System;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public interface ITxBuildRepository
    {
        Task<TxBuild> GetAsync(Guid operationId);

        Task AddAsync(TxBuild build);

        Task DeleteAsync(Guid operationId);
    }
}
