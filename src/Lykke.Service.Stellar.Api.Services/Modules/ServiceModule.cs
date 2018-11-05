using System;
using System.Globalization;
using Autofac;
using Autofac.Core;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Stellar.Api.Services.Assets;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Services.Transaction;
using Lykke.Service.Stellar.Api.Services.Horizon;
using Lykke.Service.Stellar.Api.Services.Balance;

namespace Lykke.Service.Stellar.Api.Services.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<StellarApiSettings> _settings;

        public ServiceModule(IReloadingManager<StellarApiSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HealthService>()
                   .As<IHealthService>()
                   .SingleInstance();

            builder.RegisterType<StartupManager>()
                   .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                   .As<IShutdownManager>();
            
            builder.RegisterType<HorizonService>()
                   .As<IHorizonService>()
                   .WithParameter("network", _settings.CurrentValue.NetworkPassphrase)
                   .WithParameter("horizonUrl", _settings.CurrentValue.HorizonUrl)
                   .SingleInstance();

            builder.RegisterType<BalanceService>()
                   .As<IBalanceService>()
                   .WithParameter("depositBaseAddress", _settings.CurrentValue.DepositBaseAddress)
                   .WithParameter("explorerUrlFormats", _settings.CurrentValue.ExplorerUrlFormats)
                   .SingleInstance();

            builder.RegisterType<TransactionService>()
                   .As<ITransactionService>()
                .WithParameter("transactionExpirationTime", _settings.CurrentValue.TransactionExpirationTime)
                   .SingleInstance();

            builder.RegisterType<TransactionHistoryService>()
                   .As<ITransactionHistoryService>()
                   .SingleInstance(); 

            var nativeAsset = _settings.CurrentValue.NativeAsset;
            builder.RegisterType<BlockchainAssetsService>()
                .As<IBlockchainAssetsService>()
                .WithParameters(new Parameter[]
                {
                    new NamedParameter("id", nativeAsset.Id),
                    new NamedParameter("address", nativeAsset.Address),
                    new NamedParameter("name", nativeAsset.Name),
                    new NamedParameter("typeName", nativeAsset.TypeName),
                    new NamedParameter("accuracy", nativeAsset.Accuracy)
                })
                .SingleInstance();
        }
    }
}
