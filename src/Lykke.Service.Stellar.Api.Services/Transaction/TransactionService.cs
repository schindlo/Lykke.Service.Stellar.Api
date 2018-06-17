using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StellarBase;
using StellarBase.Generated;
using StellarSdk.Exceptions;
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
        private string _lastJobError;

        private readonly IBalanceService _balanceService;
        private readonly IHorizonService _horizonService;
        private readonly IObservationRepository<BroadcastObservation> _observationRepository;
        private readonly IWalletBalanceRepository _balanceRepository;
        private readonly ITxBroadcastRepository _broadcastRepository;
        private readonly ITxBuildRepository _buildRepository;

        [UsedImplicitly]
        public TransactionService(IBalanceService balanceService,
                                  IHorizonService horizonService,
                                  IObservationRepository<BroadcastObservation> observationRepository,
                                  IWalletBalanceRepository balanceRepository,
                                  ITxBroadcastRepository broadcastRepository,
                                  ITxBuildRepository buildRepository)
        {
            _balanceService = balanceService;
            _horizonService = horizonService;
            _observationRepository = observationRepository;
            _balanceRepository = balanceRepository;
            _broadcastRepository = broadcastRepository;
            _buildRepository = buildRepository;
        }

        public async Task<TxBroadcast> GetTxBroadcastAsync(Guid operationId)
        {
            return await _broadcastRepository.GetAsync(operationId);
        }

        public async Task BroadcastTxAsync(Guid operationId, string xdrBase64)
        {
            long amount = 0;

            try
            {
                var xdr = Convert.FromBase64String(xdrBase64);
                var reader = new ByteReader(xdr);
                var txEnvelope = TransactionEnvelope.Decode(reader);
                var tx = txEnvelope.Tx;

                if (!await ProcessDwToHwTransaction(operationId, tx))
                {
                    amount = tx?.Operations?.FirstOrDefault()?.Body?.PaymentOp?.Amount?.InnerValue ?? 0;
                    var hash = await _horizonService.SubmitTransactionAsync(xdrBase64);
                    var broadcast = new TxBroadcast
                    {
                        OperationId = operationId,
                        State = TxBroadcastState.InProgress,
                        Amount = amount,
                        Hash = hash,
                        CreatedAt = DateTime.UtcNow
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
                    Amount = amount,
                    CreatedAt = DateTime.UtcNow,
                    Error = GetErrorMessage(ex),
                    ErrorCode = GetErrorCode(ex)
                };
                await _broadcastRepository.InsertOrReplaceAsync(broadcast);

                throw new BusinessException($"Broadcasting transaction failed. operationId={operationId}, message={broadcast.Error}", ex, broadcast.ErrorCode.ToString());
            }
        }

        private async Task<bool> ProcessDwToHwTransaction(Guid operationId, StellarBase.Generated.Transaction tx)
        {
            var fromKeyPair = KeyPair.FromXdrPublicKey(tx.SourceAccount.InnerValue);
            if (!_balanceService.IsDepositBaseAddress(fromKeyPair.Address) || tx.Operations.Length != 1 ||
                tx.Operations[0].Body.PaymentOp == null || string.IsNullOrWhiteSpace(tx.Memo.Text)) return false;

            var toKeyPair = KeyPair.FromXdrPublicKey(tx.Operations[0].Body.PaymentOp.Destination.InnerValue);
            if (!_balanceService.IsDepositBaseAddress(toKeyPair.Address)) return false;

            var fromAddress = $"{fromKeyPair.Address}{Constants.PublicAddressExtension.Separator}{tx.Memo.Text}";
            var amount = tx.Operations[0].Body.PaymentOp.Amount.InnerValue;
            var hash = _horizonService.GetTransactionHash(tx);
            var ledger = await _horizonService.GetLatestLedger();

            var broadcast = new TxBroadcast
            {
                OperationId = operationId,
                Amount = amount,
                Fee = 0,
                Hash = hash,
                // ReSharper disable once ArrangeRedundantParentheses
                Ledger = (ledger.Sequence * 10) + 1,
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

        private static string GetErrorMessage(Exception ex)
        {
            var errorMessage = ex.Message;
            // ReSharper disable once InvertIf
            if (ex is BadRequestException badRequest)
            {
                var resultCodes = JsonConvert.SerializeObject(badRequest.ErrorDetails.Extras.ResultCodes);
                errorMessage += $". ResultCodes={resultCodes}";
            }
            return errorMessage;
        }

        private static TxExecutionError GetErrorCode(Exception ex)
        {
            var bre = ex as BadRequestException;
            var ops = bre?.ErrorDetails?.Extras?.ResultCodes?.Operations;
            if (bre?.ErrorDetails != null && bre.ErrorDetails.Status == (int)HttpStatusCode.BadRequest &&
                ops != null && ops.Length > 0 && ops[0].Equals(StellarSdkConstants.OperationUnderfunded))
            {
                return TxExecutionError.NotEnoughBalance;
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
                else if (!_balanceService.IsDepositBaseAddress(from.Address))
                {
                    operation = new AccountMergeOperation.Builder(toKeyPair)
                                                         .SetSourceAccount(fromKeyPair)
                                                         .Build();
                }
                else
                {
                    throw new BusinessException($"It isn't allowed to merge the entire balance from the deposit base into another account! Transfer less funds. transferable={transferableBalance}");
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
                    throw new BusinessException($"It isn't possible to merge the entire balance into an unused account! Use a destination in existance. transferable={transferableBalance}");
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

        public async Task<int> UpdateBroadcastsInProgress(int batchSize)
        {
            var count = 0;

            try
            {
                string continuationToken = null;
                do
                {
                    var observations = await _observationRepository.GetAllAsync(batchSize, continuationToken);
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
                broadcast.Ledger = tx.Ledger * 10;
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
