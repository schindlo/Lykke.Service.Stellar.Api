﻿using System;
using System.Net;
using System.Threading.Tasks;
using StellarBase;
using StellarBase.Generated;
using StellarSdk.Exceptions;
using Common.Log;
using Lykke.Service.Stellar.Api.Core;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Newtonsoft.Json;

namespace Lykke.Service.Stellar.Api.Services.Transaction
{
    public class TransactionService : ITransactionService
    {
        private int _batchSize;

        private string _lastJobError;

        private readonly IBalanceService _balanceService;
        private readonly IHorizonService _horizonService;
        private readonly IObservationRepository<BroadcastObservation> _observationRepository;
        private readonly IWalletBalanceRepository _balanceRepository;
        private readonly ITxBroadcastRepository _broadcastRepository;
        private readonly ITxBuildRepository _buildRepository;
        private readonly ILog _log;

        public TransactionService(IBalanceService balanceService, IHorizonService horizonService, IObservationRepository<BroadcastObservation> observationRepository,
                                  IWalletBalanceRepository balanceRepository, ITxBroadcastRepository broadcastRepository, ITxBuildRepository buildRepository, ILog log, int batchSize)
        {
            _balanceService = balanceService;
            _horizonService = horizonService;
            _observationRepository = observationRepository;
            _balanceRepository = balanceRepository;
            _broadcastRepository = broadcastRepository;
            _buildRepository = buildRepository;
            _log = log;
            _batchSize = batchSize;
        }

        public async Task<TxBroadcast> GetTxBroadcastAsync(Guid operationId)
        {
            return await _broadcastRepository.GetAsync(operationId);
        }

        public async Task BroadcastTxAsync(Guid operationId, string xdrBase64)
        {
            try
            {
                if (!await ProcessDwToHwTransaction(operationId, xdrBase64))
                {
                    var hash = await _horizonService.SubmitTransactionAsync(xdrBase64);
                    var broadcast = new TxBroadcast
                    {
                        OperationId = operationId,
                        State = TxBroadcastState.InProgress,
                        Hash = hash
                    };
                    await _broadcastRepository.InsertOrReplaceAsync(broadcast);
                    var observation = new BroadcastObservation
                    {
                        OperationId = operationId
                    };
                    await _observationRepository.InsertOrReplaceAsync(observation);   
                }
            }
            catch (Exception ex)
            {
                var broadcast = new TxBroadcast
                {
                    OperationId = operationId,
                    State = TxBroadcastState.Failed,
                    Error = GetErrorMessage(ex),
                    ErrorCode = GetErrorCode(ex)
                };
                await _broadcastRepository.InsertOrReplaceAsync(broadcast);

                throw new BusinessException($"Broadcasting transaction failed. operationId={operationId}, message={broadcast.Error}", ex, broadcast.ErrorCode.ToString());
            }
        }

        private async Task<bool> ProcessDwToHwTransaction(Guid operationId, string xdrBase64)
        {
            var xdr = Convert.FromBase64String(xdrBase64);
            var reader = new ByteReader(xdr);
            var txEnvelope = TransactionEnvelope.Decode(reader);
            var tx = txEnvelope.Tx;

            var fromKeyPair = KeyPair.FromXdrPublicKey(tx.SourceAccount.InnerValue);
            if (_balanceService.GetDepositBaseAddress().Equals(fromKeyPair.Address, StringComparison.OrdinalIgnoreCase) &&
                tx.Operations.Length == 1 && tx.Operations[0].Body.PaymentOp != null && !string.IsNullOrWhiteSpace(tx.Memo.Text))
            {
                var toKeyPair = KeyPair.FromXdrPublicKey(tx.Operations[0].Body.PaymentOp.Destination.InnerValue);
                if (_balanceService.GetDepositBaseAddress().Equals(toKeyPair.Address, StringComparison.OrdinalIgnoreCase))
                {
                    var fromAddress = $"{fromKeyPair.Address}{Constants.PublicAddressExtension.Separator}{tx.Memo.Text}";
                    var hash = _horizonService.GetTransactionHash(tx);
                    var amount = tx.Operations[0].Body.PaymentOp.Amount.InnerValue;

                    var broadcast = new TxBroadcast
                    {
                        OperationId = operationId,
                        Amount = amount,
                        Fee = 0,
                        Hash = hash,
                        CreatedAt = DateTime.UtcNow
                    };

                    var assetId = Core.Domain.Asset.Stellar.Id;
                    if (await _balanceRepository.DecreaseBalanceAsync(assetId, fromAddress, hash, amount))
                    {
                        broadcast.State = TxBroadcastState.Completed;
                    }
                    else
                    {
                        broadcast.State = TxBroadcastState.Failed;
                        broadcast.Error = "Not enough balance!";
                        broadcast.ErrorCode = TxExecutionError.NotEnoughBalance;
                    }

                    await _broadcastRepository.InsertOrReplaceAsync(broadcast);
                    await _balanceRepository.DeleteIfBalanceIsZero(assetId, fromAddress);
                    return true;   
                }
            }

            return false;
        }

        private string GetErrorMessage(Exception ex)
        {
            var errorMessage = ex.Message;
            // handle bad request
            var badRequest = ex as BadRequestException;
            if (badRequest != null)
            {
                var resultCodes = JsonConvert.SerializeObject(badRequest.ErrorDetails.Extras.ResultCodes);
                errorMessage += $". ResultCodes={resultCodes}";
            }
            return errorMessage;
        }

        private TxExecutionError GetErrorCode(Exception ex)
        {
            if (ex.GetType() == typeof(BadRequestException))
            {
                var bre = (BadRequestException)ex;
                var ops = bre.ErrorDetails?.Extras?.ResultCodes?.Operations;
                if (bre.ErrorDetails.Status == (int)HttpStatusCode.BadRequest &&
                    ops != null && ops.Length > 0 && ops[0].Equals(StellarSdkConstants.OperationUnderfunded))
                {
                    return TxExecutionError.NotEnoughBalance;
                }
            }
            return TxExecutionError.Unknown;
        }

        public async Task DeleteTxBroadcastAsync(Guid operationId)
        {
            var deleteObservationTask = _observationRepository.DeleteIfExistAsync(operationId.ToString());
            var deleteBroadcastTask = _broadcastRepository.DeleteAsync(operationId);
            await Task.WhenAll(deleteObservationTask, deleteBroadcastTask);
        }

        public async Task<Fees> GetFeesAsync()
        {
            var latest = await _horizonService.GetLatestLedger();
            var fees = new Fees
            {
                BaseFee = latest.BaseFee,
                BaseReserve = Convert.ToDecimal(latest.BaseReserve)
            };
            return fees;
        }

        public async Task<TxBuild> GetTxBuildAsync(Guid operationId)
        {
            return await _buildRepository.GetAsync(operationId);
        }

        public async Task<string> BuildTransactionAsync(Guid operationId, AddressBalance from, string toAddress, string memoText, long amount)
        {
            var fromKeyPair = KeyPair.FromAddress(from.Address);
            var fromAccount = new Account(fromKeyPair, from.Sequence);

            var toKeyPair = KeyPair.FromAddress(toAddress);

            var transferableBalance = from.Balance - from.MinBalance;

            StellarBase.Operation operation;
            if (await _horizonService.AccountExists(toAddress))
            {
                if (amount <= transferableBalance)
                {
                    var asset = new StellarBase.Asset();
                    operation = new PaymentOperation.Builder(toKeyPair, asset, amount)
                                                    .SetSourceAccount(fromKeyPair)
                                                    .Build();
                }
                else
                {
                    operation = new AccountMergeOperation.Builder(toKeyPair)
                                                         .SetSourceAccount(fromKeyPair)
                                                         .Build();
                }
            }
            else
            {
                if (amount <= transferableBalance)
                {
                    operation = new CreateAccountOperation.Builder(toKeyPair, amount)
                                                      .SetSourceAccount(fromKeyPair)
                                                      .Build();
                }
                else
                {
                    throw new BusinessException($"Currently not possible to transfer entire balance to an unused account! Use a destination in existance. transferable={transferableBalance}");
                }
            }

            fromAccount.IncrementSequenceNumber();

            var builder = new StellarBase.Transaction.Builder(fromAccount)
                                         .AddOperation(operation);
            if (!string.IsNullOrWhiteSpace(memoText))
            {
                var memo = StellarBase.Memo.MemoText(memoText);
                builder = builder.AddMemo(memo);
            }
            var tx = builder.Build();

            var xdr = tx.ToXDR();
            var writer = new ByteWriter();
            StellarBase.Generated.Transaction.Encode(writer, xdr);
            var xdrBase64 = Convert.ToBase64String(writer.ToArray());

            var build = new TxBuild
            {
                OperationId = operationId,
                XdrBase64 = xdrBase64
            };
            await _buildRepository.AddAsync(build);

            return xdrBase64;
        }

        public string GetLastJobError()
        {
            return _lastJobError;
        }

        public async Task<int> UpdateBroadcastsInProgress()
        {
            int count = 0;

            try
            {
                string continuationToken = null;
                do
                {
                    var observations = await _observationRepository.GetAllAsync(_batchSize, continuationToken);
                    foreach (var item in observations.Items)
                    {
                        await ProcessBroadcastInProgress(item.OperationId);
                        count++;
                    }
                    continuationToken = observations.ContinuationToken;
                } while (continuationToken != null);

                _lastJobError = null;
            }
            catch (Exception ex)
            {
                _lastJobError = $"Error in job {nameof(TransactionService)}.{nameof(UpdateBroadcastsInProgress)}: {ex.Message}";
                throw new JobExecutionException("Failed to execute broadcast in progress updates", ex, count);
            }

            return count;
        }

        private async Task ProcessBroadcastInProgress(Guid operationId)
        {
            try
            {
                var broadcast = await _broadcastRepository.GetAsync(operationId);
                if (broadcast == null)
                {
                    throw new BusinessException($"Broadcast for observed operation not found. operationId={operationId})");
                }

                var tx = await _horizonService.GetTransactionDetails(broadcast.Hash);
                if (tx == null)
                {
                    // transaction still in progress
                    return;
                }

                var paymentOp = _horizonService.GetFirstPaymentFromTransaction(tx);
                broadcast.State = TxBroadcastState.Completed;
                broadcast.Amount = paymentOp.Amount.InnerValue;
                broadcast.Fee = tx.FeePaid;
                broadcast.CreatedAt = tx.CreatedAt;
                broadcast.Ledger = tx.Ledger;
                await _broadcastRepository.MergeAsync(broadcast);
                await _observationRepository.DeleteIfExistAsync(operationId.ToString());
            }
            catch (Exception ex)
            {
                var broadcast = new TxBroadcast
                {
                    State = TxBroadcastState.Failed,
                    Error = ex.Message,
                    ErrorCode = TxExecutionError.Unknown
                };
                await _broadcastRepository.MergeAsync(broadcast);
                await _observationRepository.DeleteIfExistAsync(operationId.ToString());

                throw new BusinessException($"Failed to process in progress broadcast. operationId={operationId}", ex);
            }
        }
    }
}