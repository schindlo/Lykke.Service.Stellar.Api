using System;
using System.Threading.Tasks;
using StellarBase = Stellar;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using StellarSdk.Model;
using System.Collections.Generic;

namespace Lykke.Service.Stellar.Api.Services.Transaction
{
    public class TransactionHistoryService : ITransactionHistoryService
    {
        private const int BatchSize = 100;

        private string _lastJobError;

        private readonly IHorizonService _horizonService;
        private readonly IObservationRepository<TransactionObservation> _observationRepository;
        private readonly ITxHistoryRepository _txHistoryRepository;
        private readonly ILog _log;

        public TransactionHistoryService(IHorizonService horizonService, IObservationRepository<TransactionObservation> observationRepository, ITxHistoryRepository txHistoryRepository, ILog log)
        {
            _horizonService = horizonService;
            _observationRepository = observationRepository;
            _txHistoryRepository = txHistoryRepository;
            _log = log;
        }

        public async Task<bool> IsIncomingTransactionObservedAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            return observation != null && observation.IsIncomingObserved;
        }

        public async Task<bool> IsOutgoingTransactionObservedAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            return observation != null && observation.IsOutgoingObserved;
        }

        public async Task AddIncomingTransactionObservationAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            if (observation == null)
            {
                observation = new TransactionObservation
                {
                    Address = address,
                };
            }
            observation.IsIncomingObserved = true;
            
            await _observationRepository.InsertOrReplaceAsync(observation);
        }

        public async Task AddOutgoingTransactionObservationAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            if (observation == null)
            {
                observation = new TransactionObservation
                {
                    Address = address,
                };
            }
            observation.IsOutgoingObserved = true;

            await _observationRepository.InsertOrReplaceAsync(observation);
        }

        public async Task DeleteIncomingTransactionObservationAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            if(observation == null)
            {
                // nothing to do
                return;
            }
            observation.IsIncomingObserved = false;
            if (observation.IsIncomingObserved == false && observation.IsOutgoingObserved == false)
            {
                await _txHistoryRepository.DeleteAsync(address);
                await _observationRepository.DeleteAsync(address);
            }
            else
            {
                await _observationRepository.InsertOrReplaceAsync(observation);
            }
        }

        public async Task DeleteOutgoingTransactionObservationAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            if (observation == null)
            {
                // nothing to do
                return;
            }
            observation.IsOutgoingObserved = false;
            if (observation.IsIncomingObserved == false && observation.IsOutgoingObserved == false)
            {
                await _txHistoryRepository.DeleteAsync(address);
                await _observationRepository.DeleteAsync(address);
            }
            else
            {
                await _observationRepository.InsertOrReplaceAsync(observation);
            }
        }

        public async Task<List<TxHistory>> GetHistory(TxDirectionType direction, string address, int take, string afterHash)
        {
            var result = await _txHistoryRepository.GetAllAfterHashAsync(direction, address, take, afterHash);
            return result;
        }

        public string GetLastJobError()
        {
            return _lastJobError;
        }

        public async Task UpdateTransactionHistory()
        {
            try
            {
                string continuationToken = null;
                do
                {
                    var observations = await _observationRepository.GetAllAsync(BatchSize, continuationToken);
                    foreach (var item in observations.Items)
                    {
                        await ProcessTransactionObservation(item);
                    }
                    continuationToken = observations.ContinuationToken;
                } while (continuationToken != null);

                _lastJobError = null;
            }
            catch (Exception ex)
            {
                _lastJobError = _lastJobError = $"Error in job {nameof(TransactionHistoryService)}.{nameof(UpdateTransactionHistory)}: {ex.Message}";
                await _log.WriteErrorAsync(nameof(TransactionHistoryService), nameof(UpdateTransactionHistory),
                    "Failed to execute transaction history update", ex);
            }
        }

        private string GetMemo(TransactionDetails tx)
        {
            if (("text".Equals(tx.MemoType, StringComparison.OrdinalIgnoreCase) ||
                "id".Equals(tx.MemoType, StringComparison.OrdinalIgnoreCase)) &&
                !string.IsNullOrEmpty(tx.Memo))
            {
                return tx.Memo;
            }

            return null;
        }

        private async Task QueryAndProcessPayments(string address, PaymentContext context)
        {
            var payments = await _horizonService.GetPayments(address, "asc", context.Cursor);

            context.Cursor = null;
            foreach (var payment in payments.Embedded.Records)
            {
                try
                {
                    context.Cursor = payment.PagingToken;

                    // create_account, payment or account_merge
                    if (payment.TypeI == 0 ||
                        payment.TypeI == 1 && "native".Equals(payment.AssetType, StringComparison.OrdinalIgnoreCase) ||
                        payment.TypeI == 8)
                    {
                        if (context.Transaction == null || !context.Transaction.Hash.Equals(payment.TransactionHash, StringComparison.OrdinalIgnoreCase))
                        {
                            context.Transaction = await _horizonService.GetTransactionDetails(payment.TransactionHash);
                            context.AccountMerge = 0;
                        }

                        var history = new TxHistory
                        {
                            AssetId = Asset.Stellar.Id,
                            Hash = payment.TransactionHash,
                            PaymentOperationId = payment.Id,
                            CreatedAt = payment.CreatedAt,
                            Memo = GetMemo(context.Transaction)
                        };

                        // create_account
                        if (payment.TypeI == 0)
                        {
                            history.FromAddress = payment.Funder;
                            history.ToAddress = payment.Account;
                            history.PaymentType = PaymentType.CreateAccount;

                            decimal amount = Decimal.Parse(payment.StartingBalance);
                            history.Amount = Convert.ToInt64(amount * StellarBase.One.Value);
                        }
                        // payment
                        else if (payment.TypeI == 1)
                        {
                            history.FromAddress = payment.From;
                            history.ToAddress = payment.To;
                            history.PaymentType = PaymentType.Payment;

                            decimal amount = Decimal.Parse(payment.Amount);
                            history.Amount = Convert.ToInt64(amount * StellarBase.One.Value);
                        }
                        // account_merge
                        else if (payment.TypeI == 8)
                        {
                            history.FromAddress = payment.Account;
                            history.ToAddress = payment.Into;
                            history.PaymentType = PaymentType.AccountMerge;

                            var resultXdrBase64 = context.Transaction.ResultXdr;
                            history.Amount = _horizonService.GetAccountMergeAmount(resultXdrBase64, context.AccountMerge);
                            context.AccountMerge++;
                        }
                        else
                        {
                            throw new BusinessException($"Invalid payment type: ${payment.TypeI}");
                        }

                        // TODO: map operation id
                        if (address.Equals(history.ToAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            history.Sequence = context.Sequence;
                            await _txHistoryRepository.InsertOrReplaceAsync(TxDirectionType.Incoming, history);
                            context.Sequence++;

                        }
                        if (address.Equals(history.FromAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            history.Sequence = context.Sequence;
                            await _txHistoryRepository.InsertOrReplaceAsync(TxDirectionType.Outgoing, history);
                            context.Sequence++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new BusinessException($"Failed to process payment {payment?.Id} of transaction {context?.Transaction?.Hash}.", ex);
                }
            }
        }

        private async Task ProcessTransactionObservation(TransactionObservation observation)
        {
            try
            {
                var context = new PaymentContext();
                var top = await _txHistoryRepository.GetLastRecordAsync(observation.Address);
                if (top != null)
                {
                    context.Cursor = top.PaymentOperationId;
                    context.Sequence = top.Sequence + 1;
                }

                do
                {
                    await QueryAndProcessPayments(observation.Address, context);
                }
                while (!string.IsNullOrEmpty(context.Cursor));
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(TransactionHistoryService), nameof(ProcessTransactionObservation),
                    $"Failed to process transaction observation for address: {observation.Address}", ex);
            }
        }
    }
}
