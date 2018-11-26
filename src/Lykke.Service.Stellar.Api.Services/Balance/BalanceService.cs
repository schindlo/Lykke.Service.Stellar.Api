extern alias sdk2;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Service.Stellar.Api.Core;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Utils;
using sdk2::stellar_dotnet_sdk.responses.operations;
using StellarBase;
using StellarBase.Generated;

namespace Lykke.Service.Stellar.Api.Services.Balance
{
    public class BalanceService : IBalanceService
    {
        private string _lastJobError;
        private readonly HashSet<char> _forbiddenAddressChars = new HashSet<char>
        {
            '\\',
            '/',
            '#',
            '\\',
            '?',
            '\t',
            '\r',
            '\n',
            '\0',
            '\a',
            '\b',
        };

        private readonly IHorizonService _horizonService;
        private readonly IKeyValueStoreRepository _keyValueStoreRepository;
        private readonly IObservationRepository<BalanceObservation> _observationRepository;
        private readonly IWalletBalanceRepository _walletBalanceRepository;

        private readonly string _depositBaseAddress;
        private readonly string[] _explorerUrlFormats;
        private readonly ILog _log;
        private readonly IBlockchainAssetsService _blockchainAssetsService;

        [UsedImplicitly]
        public BalanceService(IHorizonService horizonService,
                              IKeyValueStoreRepository keyValueStoreRepository,
                              IObservationRepository<BalanceObservation> observationRepository,
                              IWalletBalanceRepository walletBalanceRepository,
                              string depositBaseAddress,
                              string[] explorerUrlFormats,
                              ILogFactory log,
                              IBlockchainAssetsService blockchainAssetsService)
        {
            _horizonService = horizonService;
            _keyValueStoreRepository = keyValueStoreRepository;
            _observationRepository = observationRepository;
            _walletBalanceRepository = walletBalanceRepository;
            _depositBaseAddress = depositBaseAddress;
            _explorerUrlFormats = explorerUrlFormats;
            _log = log.CreateLog(this);
            _blockchainAssetsService = blockchainAssetsService;
        }

        public bool IsAddressValid(string address)
        {
            return IsAddressValid(address, out _);
        }

        public bool IsAddressValid(string address, out bool hasExtension)
        {
            hasExtension = false;
            bool containsForbiddenChar = false;

            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            containsForbiddenChar = address.Any(_forbiddenAddressChars.Contains);

            if (containsForbiddenChar)
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
            catch (Exception)
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
                var nativeBalance = accountDetails.Balances.Single(b => _blockchainAssetsService.GetNativeAsset().TypeName.Equals(b.AssetType, StringComparison.OrdinalIgnoreCase));
                result.Balance = Convert.ToInt64(decimal.Parse(nativeBalance.Balance, CultureInfo.InvariantCulture) * One.Value);
            }
            else
            {
                var walletBalance = await _walletBalanceRepository.GetAsync(_blockchainAssetsService.GetNativeAsset().Id, address);
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
            await _walletBalanceRepository.DeleteIfExistAsync(_blockchainAssetsService.GetNativeAsset().Id, address);
        }

        public async Task<(List<WalletBalance> Wallets, string ContinuationToken)>
            GetBalancesAsync(int take, string continuationToken)
        {
            var ledger = await _horizonService.GetLatestLedger();
            var currentLedger = (ledger.Sequence * 10) + 1;
            var balances = await _walletBalanceRepository.GetAllAsync(take, continuationToken);

            if (balances.Entities != null &&
                balances.Entities.Count != 0)
            {
                foreach (var balance in balances.Entities)
                {
                    balance.Ledger = currentLedger;
                }
            }

            return balances;
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
            var walletsToRefresh = new HashSet<(string assetId, string address)>();
            var asset = Core.Domain.Asset.Stellar;
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

                    // transaction XDR doesn't contain operation IDs,
                    // make a dedicated request to get operations
                    var operations = await _horizonService.GetTransactionOperations(transaction.Hash);
                    if (operations == null)
                    {
                        continue;
                    }

                    foreach (var op in operations)
                    {
                        string toAddress = null;
                        long amount = 0;

                        switch (op.Type.ToLower())
                        {
                            case "payment":
                                var payment = (PaymentOperationResponse)op;
                                if (payment.AssetType == "native")
                                {
                                    toAddress = payment.To.Address;
                                    amount = asset.ParseDecimal(payment.Amount);
                                }
                                break;

                            case "account_merge":
                                var accountMerge = (AccountMergeOperationResponse)op;
                                toAddress = accountMerge.Into.Address;
                                amount = _horizonService.GetAccountMergeAmount(transaction.ResultMetaXdr, accountMerge.SourceAccount.Address);
                                break;

                            case "path_payment":
                                var pathPayment = (PathPaymentOperationResponse)op;
                                if (pathPayment.AssetType == "native")
                                {
                                    toAddress = pathPayment.To.Address;
                                    amount = asset.ParseDecimal(pathPayment.Amount);
                                }
                                break;

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

                        await _walletBalanceRepository.RecordOperationAsync(asset.Id, addressWithExtension, transaction.Ledger * 10, op.Id, transaction.Hash, amount);
                        walletsToRefresh.Add((asset.Id, addressWithExtension));
                    }
                }
                catch (Exception ex)
                {
                    throw new BusinessException($"Failed to process transaction. hash={transaction?.Hash}", ex);
                }
            }

            await _walletBalanceRepository.RefreshBalance(walletsToRefresh);

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
