using System;
using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.Services
{
    public class TransactionObservationService : ITransactionObservationService
    {
        private readonly IObservationRepository<TransactionObservation> _txObservationService;

        public TransactionObservationService(IObservationRepository<TransactionObservation> txObservationService)
        {
            _txObservationService = txObservationService;
        }

        public async Task<bool> IsIncomingTransactionObservedAsync(string address)
        {
            var observation = await _txObservationService.GetAsync(address);
            return observation != null && observation.IsIncomingObserved;
        }

        public async Task<bool> IsOutgoingTransactionObservedAsync(string address)
        {
            var observation = await _txObservationService.GetAsync(address);
            return observation != null && observation.IsOutgoingObserved;
        }

        public async Task AddIncomingTransactionObservationAsync(string address)
        {
            var observation = await _txObservationService.GetAsync(address);
            if (observation == null)
            {
                observation = new TransactionObservation
                {
                    Address = address,
                };
            }
            observation.IsIncomingObserved = true;
            
            await _txObservationService.InsertOrReplaceAsync(observation);
        }

        public async Task AddOutgoingTransactionObservationAsync(string address)
        {
            var observation = await _txObservationService.GetAsync(address);
            if (observation == null)
            {
                observation = new TransactionObservation
                {
                    Address = address,
                };
            }
            observation.IsOutgoingObserved = true;

            await _txObservationService.InsertOrReplaceAsync(observation);
        }

        public async Task DeleteIncomingTransactionObservationAsync(string address)
        {
            var observation = await _txObservationService.GetAsync(address);
            if(observation == null)
            {
                // nothing to do
                return;
            }
            observation.IsIncomingObserved = false;
            if (observation.IsIncomingObserved == false && observation.IsOutgoingObserved == false)
            {
                await _txObservationService.DeleteAsync(address);
            }
            else
            {
                await _txObservationService.InsertOrReplaceAsync(observation);
            }
        }

        public async Task DeleteOutgoingTransactionObservationAsync(string address)
        {
            var observation = await _txObservationService.GetAsync(address);
            if (observation == null)
            {
                // nothing to do
                return;
            }
            observation.IsOutgoingObserved = false;
            if (observation.IsIncomingObserved == false && observation.IsOutgoingObserved == false)
            {
                await _txObservationService.DeleteAsync(address);
            }
            else
            {
                await _txObservationService.InsertOrReplaceAsync(observation);
            }
        }

        public async Task UpdateTransactionHistory()
        {
            // TODO
        }
    }
}
