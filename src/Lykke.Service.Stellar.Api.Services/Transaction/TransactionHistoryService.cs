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

        public string _LastJobError { get; private set; }

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
            return _LastJobError;
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

                _LastJobError = null;
            }
            catch (Exception ex)
            {
                _LastJobError = "Error in job " + nameof(TransactionHistoryService) + "." + nameof(UpdateTransactionHistory) +
                    ": " + ex.Message;
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

        private async Task<TransactionDetails> QueryAndProcessPayments(string address, PaymentCursor cursor, TransactionDetails lastTx)
        {
            var payments = await _horizonService.GetPayments(address, "asc", cursor.Cursor);

            TransactionDetails tx = lastTx;
            cursor.Cursor = null;
            foreach (var payment in payments.Embedded.Records)
            {
                cursor.Cursor = payment.PagingToken;

                // create_account, payment or account_merge
                if (payment.TypeI == 0 || 
                    payment.TypeI == 1 && "native".Equals(payment.AssetType, StringComparison.OrdinalIgnoreCase) ||
                    payment.TypeI == 8)
                {
                    if (tx == null || !tx.Hash.Equals(payment.TransactionHash, StringComparison.OrdinalIgnoreCase))
                    {
                        tx = await _horizonService.GetTransactionDetails(payment.TransactionHash);
                    }

                    var history = new TxHistory
                    {
                        AssetId = Asset.Stellar.Id,
                        Hash = payment.TransactionHash,
                        PaymentOperationId = payment.Id,
                        CreatedAt = payment.CreatedAt,
                        Memo = GetMemo(tx)
                    };

                    decimal amount = 0;
                    // create_account
                    if (payment.TypeI == 0)
                    {
                        history.FromAddress = payment.Funder;
                        history.ToAddress = payment.Account;
                        history.PaymentType = PaymentType.CreateAccount;
                        amount = Decimal.Parse(payment.StartingBalance);
                    }
                    // payment
                    else if (payment.TypeI == 1)
                    {
                        history.FromAddress = payment.From;
                        history.ToAddress = payment.To;
                        history.PaymentType = PaymentType.Payment;
                        amount = Decimal.Parse(payment.Amount);
                    }
                    // account_merge
                    else if (payment.TypeI == 8)
                    {
                        history.FromAddress = payment.Account;
                        history.ToAddress = payment.Into;
                        history.PaymentType = PaymentType.AccountMerge;
                        // TODO: find out via transaction result xdr
                        amount = 0;
                    }
                    else
                    {
                        throw new BusinessException($"Invalid payment type: ${payment.TypeI}");
                    }
                    history.Amount = Convert.ToInt64(amount * StellarBase.One.Value);

                    // TODO: map operation id
                    if (address.Equals(history.ToAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        history.Sequence = cursor.Sequence;
                        await _txHistoryRepository.InsertOrReplaceAsync(TxDirectionType.Incoming, history);
                        cursor.Sequence++;

                    }
                    if (address.Equals(history.FromAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        history.Sequence = cursor.Sequence;
                        await _txHistoryRepository.InsertOrReplaceAsync(TxDirectionType.Outgoing, history);
                        cursor.Sequence++;
                    }
                }
            }

            return tx;
        }

        private async Task ProcessTransactionObservation(TransactionObservation observation)
        {
            try
            {
                var cursor = new PaymentCursor();
                var top = await _txHistoryRepository.GetLastRecordAsync(observation.Address);
                if (top != null)
                {
                    cursor.Cursor = top.PaymentOperationId;
                    cursor.Sequence = top.Sequence + 1;
                }

                TransactionDetails lastTx = null;
                do
                {
                    lastTx = await QueryAndProcessPayments(observation.Address, cursor, lastTx);
                }
                while (!string.IsNullOrEmpty(cursor.Cursor));
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(TransactionHistoryService), nameof(ProcessTransactionObservation),
                    $"Failed to process transaction observation for address: {observation.Address}", ex);
            }
        }
    }
}
