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
        private readonly IObservationRepository<TransactionHistoryObservation> _observationRepository;
        private readonly ITxHistoryRepository _txHistoryRepository;
        private readonly ITxBroadcastRepository _txBroadcastRepository;
        private readonly ILog _log;

        public TransactionHistoryService(IHorizonService horizonService, IObservationRepository<TransactionHistoryObservation> observationRepository,
                                         ITxHistoryRepository txHistoryRepository, ITxBroadcastRepository txBroadcastRepository, ILog log)
        {
            _horizonService = horizonService;
            _observationRepository = observationRepository;
            _txHistoryRepository = txHistoryRepository;
            _txBroadcastRepository = txBroadcastRepository;
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

        private TransactionHistoryObservation CreateTransactionObservation(string address) 
        {
            var observation = new TransactionHistoryObservation
            {
                Address = address,
                TableId = Guid.NewGuid().ToString("N").ToUpper()
            };
            return observation;
        }

        public async Task AddIncomingTransactionObservationAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            if (observation == null)
            {
                observation = CreateTransactionObservation(address);
            }
            observation.IsIncomingObserved = true;
            
            await _observationRepository.InsertOrReplaceAsync(observation);
        }

        public async Task AddOutgoingTransactionObservationAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            if (observation == null)
            {
                observation = CreateTransactionObservation(address);
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
                await _txHistoryRepository.DeleteAsync(observation.TableId);
                await _observationRepository.DeleteIfExistAsync(address);
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
                await _txHistoryRepository.DeleteAsync(observation.TableId);
                await _observationRepository.DeleteIfExistAsync(address);
            }
            else
            {
                await _observationRepository.InsertOrReplaceAsync(observation);
            }
        }

        public async Task<List<TxHistory>> GetHistory(TxDirectionType direction, string address, int take, string afterHash)
        {
            var observation = await _observationRepository.GetAsync(address);
            if (observation != null)
            {
                var result = await _txHistoryRepository.GetAllAfterHashAsync(observation.TableId, direction, take, afterHash);
                return result;
            }

            return new List<TxHistory>();
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

        public string GetLastJobError()
        {
            return _lastJobError;
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
            if (payments == null)
            {
                await _log.WriteWarningAsync(nameof(TransactionHistoryService), nameof(QueryAndProcessPayments),
                    $"Address not found: {address}");
                context.Cursor = null;
                return;
            }

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
                            var tx = await _horizonService.GetTransactionDetails(payment.TransactionHash);
                            context.Transaction = tx ?? throw new BusinessException($"Transaction not found (hash: {payment.TransactionHash}).");
                            context.AccountMerge = 0;
                        }

                        var history = new TxHistory
                        {
                            AssetId = Asset.Stellar.Id,
                            Hash = payment.TransactionHash,
                            PaymentId = payment.Id,
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

                        history.OperationId = await _txBroadcastRepository.GetOperationId(payment.TransactionHash);

                        if (address.Equals(history.ToAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            await _txHistoryRepository.InsertOrReplaceAsync(context.TableId, TxDirectionType.Incoming, history);
                            context.Sequence++;

                        }
                        if (address.Equals(history.FromAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            await _txHistoryRepository.InsertOrReplaceAsync(context.TableId, TxDirectionType.Outgoing, history);
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

        private async Task ProcessTransactionObservation(TransactionHistoryObservation observation)
        {
            try
            {
                var context = new PaymentContext(observation.TableId);
                var last = await _txHistoryRepository.GetLastRecordAsync(context.TableId);
                if (last != null)
                {
                    context.Cursor = last.PaymentId;
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
