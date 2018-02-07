using System;
using System.Threading.Tasks;
using StellarBase = Stellar;
using StellarSdk;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using StellarSdk.Model;
using System.Collections.Generic;

namespace Lykke.Service.Stellar.Api.Services
{
    public class TransactionObservationService : ITransactionObservationService
    {
        private const int BatchSize = 100;

        private readonly string _horizonUrl;

        private readonly IObservationRepository<TransactionObservation> _observationRepository;
        private readonly ITxHistoryRepository _txHistoryRepository;
        private readonly ILog _log;

        public TransactionObservationService(IObservationRepository<TransactionObservation> observationRepository, ITxHistoryRepository txHistoryRepository, string horizonUrl, ILog log)
        {
            _observationRepository = observationRepository;
            _txHistoryRepository = txHistoryRepository;
            _horizonUrl = horizonUrl;
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
            // TODO: afterHash
            var result = await _txHistoryRepository.GetAllAsync(direction, address, take, null);
            return result.Items;
        }

        public async Task UpdateTransactionHistory()
        {
            string continuationToken = null;
            do
            {
                var observations = await _observationRepository.GetAllAsync(BatchSize, continuationToken);
                foreach (var item in observations.Entities)
                {
                    await ProcessTransactionObservation(item);
                }
                continuationToken = observations.ContinuationToken;
            } while (continuationToken != null);
        }

        private async Task<TransactionDetails> GetTransactionDetails(string transactionHash)
        {
            var builder = new TransactionCallBuilder(_horizonUrl);
            builder.transaction(transactionHash);
            var tx = await builder.Call();
            return tx;
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

        private async Task<(string, ulong)> QueryAndProcessPayments(string address, string cursor, ulong inverseSeq)
        {
            var builder = new PaymentCallBuilder(_horizonUrl);
            builder.accountId(address);
            builder.order("asc").cursor(cursor);
            var payments = await builder.Call();

            string nextCursor = null;
            foreach (var payment in payments.Embedded.Records)
            {
                nextCursor = payment.PagingToken;

                // create_account, payment or account_merge
                if (payment.TypeI == 0 || 
                    payment.TypeI == 1 && "native".Equals(payment.AssetType, StringComparison.OrdinalIgnoreCase) ||
                    payment.TypeI == 8)
                {
                    // TODO: cash latest tx details
                    var tx = await GetTransactionDetails(payment.TransactionHash);

                    var history = new TxHistory
                    {
                        InverseSequence = inverseSeq,
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
                        await _txHistoryRepository.InsertOrReplaceAsync(TxDirectionType.Incoming, history);

                        inverseSeq--;
                        history.InverseSequence = inverseSeq;
                    }
                    if (address.Equals(history.FromAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        await _txHistoryRepository.InsertOrReplaceAsync(TxDirectionType.Outgoing, history);
                        inverseSeq--;
                    }
                }
            }

            return (nextCursor, inverseSeq);
        }

        private async Task ProcessTransactionObservation(TransactionObservation observation)
        {
            try
            {
                string latest = string.Empty;
                ulong inverseSeq = UInt64.MaxValue;
                if (observation.IsIncomingObserved)
                {
                    var top = await _txHistoryRepository.GetTopRecordAsync(TxDirectionType.Incoming, observation.Address);
                    if (top != null)
                    {
                        latest = top.PaymentOperationId;
                        inverseSeq = top.InverseSequence - 1;
                    }
                }
                if (observation.IsOutgoingObserved)
                {
                    var top = await _txHistoryRepository.GetTopRecordAsync(TxDirectionType.Outgoing, observation.Address);
                    if (top != null)
                    {
                        var outInverseSeq = top.InverseSequence - 1;
                        if (outInverseSeq < inverseSeq)
                        {
                            latest = top.PaymentOperationId;
                            inverseSeq = outInverseSeq;
                        }
                    }
                }

                string cursor = latest.ToString();
                do
                {
                    (cursor, inverseSeq) = await QueryAndProcessPayments(observation.Address, cursor, inverseSeq);
                }
                while (cursor != null);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(TransactionObservationService), nameof(ProcessTransactionObservation),
                    $"Failed to process transaction observation for address: {observation.Address}", ex);
            }
        }
    }
}
