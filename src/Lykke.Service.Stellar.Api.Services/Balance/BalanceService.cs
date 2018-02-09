using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using StellarBase = Stellar;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.Services
{
    public class BalanceService : IBalanceService
    {
        private int _batchSize;

        private string _lastJobError;

        private readonly IHorizonService _horizonService;
        private readonly IObservationRepository<BalanceObservation> _observationRepository;
        private readonly IWalletBalanceRepository _walletBalanceRepository;
        private readonly ILog _log;

        public BalanceService(IHorizonService horizonService, IObservationRepository<BalanceObservation> observationRepository, IWalletBalanceRepository walletBalanceRepository, ILog log, int batchSize)
        {
            _horizonService = horizonService;
            _observationRepository = observationRepository;
            _walletBalanceRepository = walletBalanceRepository;
            _log = log;
            _batchSize = batchSize;
        }

        public bool IsAddressValid(string address)
        {
            try
            {
                StellarBase.StrKey.DecodeCheck(StellarBase.VersionByte.ed25519Publickey, address);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<AddressBalance> GetAddressBalanceAsync(string address, Fees fees = null)
        {
            var accountDetails = await _horizonService.GetAccountDetails(address);
            if(accountDetails == null)
            {
                // address not found
                return null;
            }

            var result = new AddressBalance
            {
                Address = address
            };
            result.Sequence = Int64.Parse(accountDetails.Sequence);

            var nativeBalance = accountDetails.Balances.Single(b => Asset.Stellar.TypeName.Equals(b.AssetType, StringComparison.OrdinalIgnoreCase));
            result.Balance = Convert.ToInt64(Decimal.Parse(nativeBalance.Balance) * StellarBase.One.Value);
            if (fees != null)
            {
                long entries = accountDetails.Signers.Length + accountDetails.SubentryCount;
                var minBalance = (2 + entries) * fees.BaseReserve * StellarBase.One.Value;
                result.MinBalance = Convert.ToInt64(minBalance);
            }

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
            await _walletBalanceRepository.DeleteIfExistAsync(address);
        }

        public async Task<(List<WalletBalance> Wallets, string ContinuationToken)> GetBalancesAsync(int take, string continuationToken)
        {
            return await _walletBalanceRepository.GetAllAsync(take, continuationToken);
        }

        public async Task<int> UpdateWalletBalances()
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
                        await ProcessWallet(item.Address);
                        count++;
                    }
                    continuationToken = observations.ContinuationToken;
                } while (continuationToken != null);

                _lastJobError = null;
            }
            catch (Exception ex)
            {
                _lastJobError = $"Error in job {nameof(BalanceService)}.{nameof(UpdateWalletBalances)}: {ex.Message}";
                await _log.WriteErrorAsync(nameof(BalanceService), nameof(UpdateWalletBalances),
                    "Failed to execute balances update", ex);
            }
            return count;
        }

        public string GetLastJobError()
        {
            return _lastJobError;
        }

        private async Task ProcessWallet(string address)
        {
            try
            {
                var addressBalance = await GetAddressBalanceAsync(address);
                if (addressBalance == null)
                {
                    await _log.WriteWarningAsync(nameof(BalanceService), nameof(ProcessWallet),
                        $"Address not found: {address}");
                    return;
                }

                if (addressBalance.Balance > 0)
                {
                    var walletEntry = await _walletBalanceRepository.GetAsync(address);
                    if (walletEntry == null)
                    {
                        walletEntry = new WalletBalance
                        {
                            Address = address
                        };
                    }
                    if (walletEntry.Balance != addressBalance.Balance)
                    {
                        walletEntry.Balance = addressBalance.Balance;
                        walletEntry.Ledger = await _horizonService.GetLedgerNoOfLastPayment(address);
                        await _walletBalanceRepository.InsertOrReplaceAsync(walletEntry);
                    }
                }
                else
                {
                    await _walletBalanceRepository.DeleteIfExistAsync(address);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(BalanceService), nameof(ProcessWallet),
                    $"Failed to process wallet {address} during balance update.", ex);
            }
        }
    }
}
