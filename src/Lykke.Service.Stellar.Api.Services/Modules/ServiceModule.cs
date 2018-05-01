using Autofac;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Settings.ServiceSettings;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.Services.Transaction;
using Lykke.Service.Stellar.Api.Services.Horizon;
using Lykke.Service.Stellar.Api.Services.Balance;

namespace Lykke.Service.Stellar.Api.Services.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<StellarApiSettings> _settings;
        private readonly ILog _log;

        public ServiceModule(IReloadingManager<StellarApiSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
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
                   .SingleInstance();

            builder.RegisterType<TransactionHistoryService>()
                   .As<ITransactionHistoryService>()
                   .SingleInstance();
        }
    }
}
