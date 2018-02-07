using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using StellarBase = Stellar;
using StellarSdk;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.Services
{
    public class BalanceService : IBalanceService
    {
        private const int BatchSize = 100;

        private readonly string _horizonUrl;

        private readonly IObservationRepository<BalanceObservation> _observationRepository;

        private readonly IWalletBalanceRepository _walletBalanceRepository;

        public BalanceService(IObservationRepository<BalanceObservation> observationRepository, IWalletBalanceRepository walletBalanceRepository, string horizonUrl)
        {
            _observationRepository = observationRepository;
            _walletBalanceRepository = walletBalanceRepository;
            _horizonUrl = horizonUrl;
        }

        public async Task<AddressBalance> GetAddressBalanceAsync(string address, Fees fees = null)
        {
            var result = new AddressBalance
            {
                Address = address
            };

            var builder = new AccountCallBuilder(_horizonUrl);
            builder.accountId(address);
            var accountDetails = await builder.Call();
            result.Sequence = Int64.Parse(accountDetails.Sequence);

            var nativeBalance = accountDetails.Balances.Single(b => "native".Equals(b.AssetType, StringComparison.OrdinalIgnoreCase));
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
            await _observationRepository.DeleteAsync(address);
            await _walletBalanceRepository.DeleteIfExistAsync(address);
        }

        public async Task<(List<WalletBalance> Wallets, string ContinuationToken)> GetBalancesAsync(int take, string continuationToken)
        {
            return await _walletBalanceRepository.GetAllAsync(take, continuationToken);
        }

        public async Task UpdateWalletBalances()
        {
            string continuationToken = null;
            do
            {
                var observations = await _observationRepository.GetAllAsync(BatchSize, continuationToken);
                foreach (var entry in observations.Entities)
                {
                    var addressBalance = await GetAddressBalanceAsync(entry.Address);
                    if (addressBalance.Balance > 0)
                    {
                        var walletEntry = await _walletBalanceRepository.GetAsync(entry.Address);
                        if (walletEntry == null)
                        {
                            walletEntry = new WalletBalance
                            {
                                Address = entry.Address
                            };
                        }
                        if (walletEntry.Balance != addressBalance.Balance)
                        {
                            walletEntry.Balance = addressBalance.Balance;
                            // TODO: find ledger of last payment
                            await _walletBalanceRepository.InsertOrReplaceAsync(walletEntry);
                        }
                    }
                    else
                    {
                        await _walletBalanceRepository.DeleteIfExistAsync(entry.Address);
                    }
                }
                continuationToken = observations.ContinuationToken;
            } while (continuationToken != null);
        }
    }
}
