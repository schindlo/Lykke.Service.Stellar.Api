using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Observation
{
    public interface IBalanceObservationRepository
    {
        Task<(List<BalanceObservation> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken);

        Task<BalanceObservation> GetAsync(string address);

        Task AddAsync(string address);

        Task DeleteAsync(string address);
    }
}
