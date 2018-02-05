using System;
using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface ITransactionObservationService
    {
        Task<bool> IsIncomingTransactionObservedAsync(string address);

        Task<bool> IsOutgoingTransactionObservedAsync(string address);

        Task AddIncomingTransactionObservationAsync(string address);

        Task AddOutgoingTransactionObservationAsync(string address);

        Task DeleteIncomingTransactionObservationAsync(string address);

        Task DeleteOutgoingTransactionObservationAsync(string address);
    }
}
