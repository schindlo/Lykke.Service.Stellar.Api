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

            builder.RegisterType<ObservationRepository<BalanceObservationEntity, BalanceObservation>>()
                .As<IObservationRepository<BalanceObservation>>()
                .WithParameter(TypedParameter.From(dataConnStringManager));

            builder.RegisterType<ObservationRepository<TransactionObservationEntity, TransactionObservation>>()
                .As<IObservationRepository<TransactionObservation>>()
                .WithParameter(TypedParameter.From(dataConnStringManager));

            builder.RegisterType<WalletBalanceRepository>()
                .As<IWalletBalanceRepository>()
                .WithParameter(TypedParameter.From(dataConnStringManager));

            builder.RegisterType<StellarService>()
                .As<IStellarService>()
                .SingleInstance();

            builder.RegisterType<BalanceService>()
                .As<IBalanceService>()
                .SingleInstance();

            builder.RegisterType<TransactionObservationService>()
                   .As<ITransactionObservationService>()
                .SingleInstance();

            builder.RegisterType<UpdateBalancesJob>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("period", 60 * 1000) // TODO: configureable
                .SingleInstance();
            
            builder.Populate(_services);
        }
    }
}
