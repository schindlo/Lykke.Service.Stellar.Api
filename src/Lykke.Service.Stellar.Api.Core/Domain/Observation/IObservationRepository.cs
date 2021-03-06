﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain.Observation
{
    public interface IObservationRepository<T>
    {
        Task<(List<T> Items, string ContinuationToken)> GetAllAsync(int take, string continuationToken);

        Task<T> GetAsync(string key);

        Task InsertOrReplaceAsync(T observation);

        Task DeleteIfExistAsync(string key);
    }
}
