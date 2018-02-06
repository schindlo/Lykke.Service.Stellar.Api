using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

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

        Task<List<TxHistory>> GetHistory(TxDirectionType direction, string address, int take, string afterHash);

        Task UpdateTransactionHistory();
    }
}
