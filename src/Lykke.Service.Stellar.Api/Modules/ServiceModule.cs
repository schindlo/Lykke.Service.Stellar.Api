using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Service.Stellar.Api.AzureRepositories.Transaction;
using Lykke.Service.Stellar.Api.AzureRepositories.Balance;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Stellar.Api.Services;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Service.Stellar.Api.Jobs;
using Lykke.Service.Stellar.Api.AzureRepositories.Observation;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Lykke.Service.Stellar.Api.Services.Transaction;
using Lykke.Service.Stellar.Api.Services.Horizon;

namespace Lykke.Service.Stellar.Api.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<StellarApiSettings> _settings;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public ServiceModule(IReloadingManager<StellarApiSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            // TODO: Do not register entire settings in container, pass necessary settings to services which requires them
            // ex:
            //  builder.RegisterType<QuotesPublisher>()
            //      .As<IQuotesPublisher>()
            //      .WithParameter(TypedParameter.From(_settings.CurrentValue.QuotesPublication))

            builder.RegisterInstance(_log)
                   .As<ILog>()
                   .SingleInstance();

            builder.RegisterType<HealthService>()
                   .As<IHealthService>()
                   .SingleInstance();

            builder.RegisterType<StartupManager>()
                   .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                   .As<IShutdownManager>();

            var dataConnStringManager = _settings.ConnectionString(x => x.Db.DataConnString);
            builder.RegisterType<TxBroadcastRepository>()
                   .As<ITxBroadcastRepository>()
                   .WithParameter(TypedParameter.From(dataConnStringManager));

            builder.RegisterType<TxBuildRepository>()
                   .As<ITxBuildRepository>()
                   .WithParameter(TypedParameter.From(dataConnStringManager));
            
            builder.RegisterType<TxHistoryRepository>()
                   .As<ITxHistoryRepository>()
                   .WithParameter(TypedParameter.From(dataConnStringManager));

            builder.RegisterType<ObservationRepository<BalanceObservationEntity, BalanceObservation>>()
                   .As<IObservationRepository<BalanceObservation>>()
                   .WithParameter(TypedParameter.From(dataConnStringManager));                             

            builder.RegisterType<ObservationRepository<TransactionHistoryObservationEntity, TransactionHistoryObservation>>()
                   .As<IObservationRepository<TransactionHistoryObservation>>()
                   .WithParameter(TypedParameter.From(dataConnStringManager));

            builder.RegisterType<ObservationRepository<BroadcastObservationEntity, BroadcastObservation>>()
                    .As<IObservationRepository<BroadcastObservation>>()
                    .WithParameter(TypedParameter.From(dataConnStringManager));

            builder.RegisterType<WalletBalanceRepository>()
                   .As<IWalletBalanceRepository>()
                   .WithParameter(TypedParameter.From(dataConnStringManager));

            builder.RegisterType<HorizonService>()
                   .As<IHorizonService>()
                   .WithParameter("horizonUrl", _settings.CurrentValue.HorizonUrl)
                   .SingleInstance();

            builder.RegisterType<BalanceService>()
                   .As<IBalanceService>()
                   .SingleInstance();

            builder.RegisterType<TransactionService>()
                   .As<ITransactionService>()
                   .SingleInstance();

            builder.RegisterType<TransactionHistoryService>()
                   .As<ITransactionHistoryService>()
                   .SingleInstance();

            builder.RegisterType<WalletBalanceJob>()
                   .As<IStartable>()
                   .AutoActivate()
                   .WithParameter("period", _settings.CurrentValue.WalletBalanceJobPeriodSeconds * 1000)
                   .SingleInstance();

            builder.RegisterType<TransactionHistoryJob>()
                   .As<IStartable>()
                   .AutoActivate()
                   .WithParameter("period", _settings.CurrentValue.TransactionHistoryJobPeriodSeconds * 1000)
                   .SingleInstance();
                
            builder.RegisterType<BroadcastInProgressJob>()
                   .As<IStartable>()
                   .AutoActivate()
                   .WithParameter("period", _settings.CurrentValue.BroadcastInProgressJobPeriodSeconds * 1000)
                   .SingleInstance();

            builder.Populate(_services);
        }
    }
}
