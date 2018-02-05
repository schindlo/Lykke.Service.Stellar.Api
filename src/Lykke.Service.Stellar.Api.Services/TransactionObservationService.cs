using System;
using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.Services
{
    public class TransactionObservationService : ITransactionObservationService
    {
        private readonly IObservationRepository<IncomingTransactionObservation> _incomingTxRepository;

        private readonly IObservationRepository<OutgoingTransactionObservation> _outgoingTxRepository;

        public TransactionObservationService(IObservationRepository<IncomingTransactionObservation> incomingTxRepository, IObservationRepository<OutgoingTransactionObservation> outgoingTxRepository)
        {
            _incomingTxRepository = incomingTxRepository;
            _outgoingTxRepository = outgoingTxRepository;
        }

        public async Task<bool> IsIncomingTransactionObservedAsync(string address)
        {
            return await _incomingTxRepository.GetAsync(address) != null;
        }

        public async Task<bool> IsOutgoingTransactionObservedAsync(string address)
        {
            return await _outgoingTxRepository.GetAsync(address) != null;
        }

        public async Task AddIncomingTransactionObservationAsync(string address)
        {
            var observation = new IncomingTransactionObservation
            {
                Address = address
            };
            await _incomingTxRepository.AddAsync(observation);
        }

        public async Task AddOutgoingTransactionObservationAsync(string address)
        {
            var observation = new OutgoingTransactionObservation
            {
                Address = address
            };
            await _outgoingTxRepository.AddAsync(observation);
        }

        public async Task DeleteIncomingTransactionObservationAsync(string address)
        {
            await _incomingTxRepository.DeleteAsync(address);
        }

        public async Task DeleteOutgoingTransactionObservationAsync(string address)
        {
            await _outgoingTxRepository.DeleteAsync(address);
        }
    }
}
