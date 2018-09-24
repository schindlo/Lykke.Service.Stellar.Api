using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.Stellar.Api.Core;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Utils;
using StellarBase;
using StellarBase.Generated;
using StellarSdk.Model;

namespace Lykke.Service.Stellar.Api.Services.Balance
{
    public class BalanceService : IBalanceService
    {
        private string _lastJobError;

        private readonly IHorizonService _horizonService;
        private readonly IKeyValueStoreRepository _keyValueStoreRepository;
        private readonly IObservationRepository<BalanceObservation> _observationRepository;
        private readonly IWalletBalanceRepository _walletBalanceRepository;

        private readonly string _depositBaseAddress;
        private readonly string[] _explorerUrlFormats;
        private readonly ILog _log;

        [UsedImplicitly]
        public BalanceService(IHorizonService horizonService,
                              IKeyValueStoreRepository keyValueStoreRepository,
                              IObservationRepository<BalanceObservation> observationRepository, 
                              IWalletBalanceRepository walletBalanceRepository,
                              string depositBaseAddress,
                              string[] explorerUrlFormats,
                              ILog log)
        {
            _horizonService = horizonService;
            _keyValueStoreRepository = keyValueStoreRepository;
            _observationRepository = observationRepository;
            _walletBalanceRepository = walletBalanceRepository;
            _depositBaseAddress = depositBaseAddress;
            _explorerUrlFormats = explorerUrlFormats;
            _log = log;
        }

        public bool IsAddressValid(string address)
        {
            return IsAddressValid(address, out _);
        }

        public bool IsAddressValid(string address, out bool hasExtension)
        {
            hasExtension = false;

            if (string.IsNullOrWhiteSpace(address)) 
            {
                return false;
            }

            var parts = address.Split(Constants.PublicAddressExtension.Separator, 2);
            try
            {
                var baseAddress = parts[0];
                byte[] secret = StrKey.DecodeCheck(VersionByte.ed25519Publickey, baseAddress);
                string encoded = StrKey.EncodeCheck(VersionByte.ed25519Publickey, secret);

                if (baseAddress != encoded)
                    return false;
            }
            catch (Exception e)
            {
                return false;
            }

            if (parts.Length > 1)
            {
                if (!IsValidMemo(parts[1])) 
                {
                    return false;   
                }

                hasExtension = true;
            }

            return true;
        }

        private bool IsValidMemo(string memo)
        {
            if (string.IsNullOrWhiteSpace(memo)) 
            {
                return false; 
            }

            var length = Encoding.UTF8.GetByteCount(memo);
            return length <= StellarSdkConstants.MaxMemoLength;
        }

        public string GetDepositBaseAddress()
        {
            return _depositBaseAddress;
        }

        public bool IsDepositBaseAddress(string address)
        {
            var baseAddress = GetBaseAddress(address);
            return GetDepositBaseAddress().Equals(baseAddress, StringComparison.OrdinalIgnoreCase);
        }

        public string GetBaseAddress(string address)
        {
            return address.Split(Constants.PublicAddressExtension.Separator, 2)[0];
        }

        public string GetPublicAddressExtension(string address)
        {
            var parts = address.Split(Constants.PublicAddressExtension.Separator, 2);
            return parts.Length > 1 ? parts[1] : null;
        }

        public async Task<AddressBalance> GetAddressBalanceAsync(string address, Fees fees = null)
        {
            var baseAddress = GetBaseAddress(address);
            var result = new AddressBalance
            {
                Address = baseAddress
            };

            var accountDetails = await _horizonService.GetAccountDetails(baseAddress);
            if (accountDetails == null)
            {
                // address not found
                return result;
            }
            result.Sequence = long.Parse(accountDetails.Sequence);

            var addressExtension = GetPublicAddressExtension(address);
            if (string.IsNullOrEmpty(addressExtension))
            {
                var nativeBalance = accountDetails.Balances.Single(b => Core.Domain.Asset.Stellar.TypeName.Equals(b.AssetType, StringComparison.OrdinalIgnoreCase));
                result.Balance = Convert.ToInt64(decimal.Parse(nativeBalance.Balance) * One.Value);
            }
            else
            {
                var walletBalance = await _walletBalanceRepository.GetAsync(Core.Domain.Asset.Stellar.Id, address);
                if (walletBalance != null)
                {
                    result.Balance = walletBalance.Balance;
                }
            }

            if (fees == null) return result;

            var entries = accountDetails.Signers.Length + accountDetails.SubentryCount;
            var minBalance = (2 + entries) * fees.BaseReserve;
            result.MinBalance = Convert.ToInt64(minBalance);

            return result;
        }

        public async Task<bool> IsBalanceObservedAsync(string address)
        {
            return await _observationRepository.GetAsync(address) != null;
        }

        public async Task AddBalanceObservationAsync(string address)
        {
            var observation = new BalanceObservation
            {
                Address = address
            };
            await _observationRepository.InsertOrReplaceAsync(observation);
        }

        public async Task DeleteBalanceObservationAsync(string address)
        {
            await _observationRepository.DeleteIfExistAsync(address);
            await _walletBalanceRepository.DeleteIfExistAsync(Core.Domain.Asset.Stellar.Id, address);
        }

        public async Task<(List<WalletBalance> Wallets, string ContinuationToken)> GetBalancesAsync(int take, string continuationToken)
        {
            return await _walletBalanceRepository.GetAllAsync(take, continuationToken);
        }

        public List<string> GetExplorerUrls(string address)
        {
            return _explorerUrlFormats.Select(format => string.Format(format, address)).ToList();
        }

        private string GetPagingTokenKey => $"TransactionPagingToken:{_depositBaseAddress}";

        public async Task<int> UpdateWalletBalances()
        {
            var count = 0;

            try
            {
                var cursor = await _keyValueStoreRepository.GetAsync(GetPagingTokenKey);

                do
                {
                    var result = await ProcessDeposits(cursor);
                    count += result.Count;
                    cursor = result.Cursor;
                }
                while (!string.IsNullOrEmpty(cursor));

                _lastJobError = null;
            }
            catch (Exception ex)
            {
                _lastJobError = $"Error in job {nameof(BalanceService)}.{nameof(UpdateWalletBalances)}: {ex.Message}";
                throw new JobExecutionException("Failed to execute balances updates", ex, count);
            }

            return count;
        }

        private async Task<(int Count, string Cursor)> ProcessDeposits(string cursor)
        {
            var transactions = await _horizonService.GetTransactions(_depositBaseAddress, StellarSdkConstants.OrderAsc, cursor);
            var count = 0;
            cursor = null;
            foreach (var transaction in transactions)
            {
                try
                {
                    cursor = transaction.PagingToken;
                    count++;

                    // skip outgoing transactions and transactions without memo
                    var memo = _horizonService.GetMemo(transaction);
                    if (_depositBaseAddress.Equals(transaction.SourceAccount, StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(memo))
                    {
                        continue;
                    }

                    var xdr = Convert.FromBase64String(transaction.EnvelopeXdr);
                    var reader = new ByteReader(xdr);
                    var txEnvelope = TransactionEnvelope.Decode(reader);
                    var tx = txEnvelope.Tx;

                    for (short i = 0; i < tx.Operations.Length; i++)
                    {
                        var operation = tx.Operations[i];
                        var operationType = operation.Body.Discriminant.InnerValue;

                        string toAddress = null;
                        long amount = 0;
                        // ReSharper disable once SwitchStatementMissingSomeCases
                        switch (operationType)
                        {
                            case OperationType.OperationTypeEnum.PAYMENT:
                            {
                                var op = operation.Body.PaymentOp;
                                if (op.Asset.Discriminant.InnerValue == AssetType.AssetTypeEnum.ASSET_TYPE_NATIVE)
                                {
                                    var keyPair = KeyPair.FromXdrPublicKey(op.Destination.InnerValue);
                                    toAddress = keyPair.Address;
                                    amount = op.Amount.InnerValue;
                                }
                                break;
                            }
                            case OperationType.OperationTypeEnum.ACCOUNT_MERGE:
                            {
                                var op = operation.Body;
                                var keyPair = KeyPair.FromXdrPublicKey(op.Destination.InnerValue);
                                toAddress = keyPair.Address;
                                amount = _horizonService.GetAccountMergeAmount(transaction.ResultXdr, i);
                                break;
                            }
                            case OperationType.OperationTypeEnum.PATH_PAYMENT:
                            {
                                var op = operation.Body.PathPaymentOp;
                                if (op.DestAsset.Discriminant.InnerValue == AssetType.AssetTypeEnum.ASSET_TYPE_NATIVE)
                                {
                                   var keyPair = KeyPair.FromXdrPublicKey(op.Destination.InnerValue);
                                   toAddress = keyPair.Address;
                                   amount = op.DestAmount.InnerValue;
                                }
                                break;
                            }
                            default:
                                continue;
                        }

                        var addressWithExtension = $"{toAddress}{Constants.PublicAddressExtension.Separator}{memo.ToLower()}";
                        if (!ForbiddenCharacterAzureStorageUtils.IsValidRowKey(memo))
                        {
                            await _log.WriteErrorAsync(nameof(BalanceService),
                                nameof(ProcessDeposits), 
                                addressWithExtension, 
                                new Exception("Possible cashin skipped. It has forbiddden characters in memo."));

                            continue;
                        }

                        var observation = await _observationRepository.GetAsync(addressWithExtension);
                        if (observation == null)
                        {
                            continue;    
                        }

                        var assetId = Core.Domain.Asset.Stellar.Id;
                        await _walletBalanceRepository.IncreaseBalanceAsync(assetId, addressWithExtension, transaction.Ledger * 10, i, transaction.Hash, amount);
                    }
                }
                catch (Exception ex)
                {
                    throw new BusinessException($"Failed to process transaction. hash={transaction?.Hash}", ex);
                }
            }

            if (!string.IsNullOrEmpty(cursor))
            {
                await _keyValueStoreRepository.SetAsync(GetPagingTokenKey, cursor);
            }

            return (count, cursor);
        }

        public string GetLastJobError()
        {
            return _lastJobError;
        }
    }
}
