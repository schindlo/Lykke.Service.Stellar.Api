using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Observation
{
    public interface IObservationRepository<T>
    {
        Task<(List<T> Entities, string ContinuationToken)> GetAllAsync(int take, string continuationToken);

        Task<T> GetAsync(string key);

        Task AddAsync(T obersvation);

        Task DeleteAsync(string key);
    }
}
