using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Job.Stellar.Api.Jobs;
using Lykke.Job.Stellar.Api.Settings;

namespace Lykke.Job.Stellar.Api.Modules
{
    public class StellarJobModule : Module
    {
        private readonly IReloadingManager<StellarJobSettings> _settings;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public StellarJobModule(IReloadingManager<StellarJobSettings> settings, ILog log)
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

            builder.RegisterType<WalletBalanceJob>()
                   .As<IStartable>()
                   .AutoActivate()
                   .WithParameter("period", _settings.CurrentValue.WalletBalanceJobPeriodSeconds * 1000)
                   .SingleInstance();

            builder.RegisterType<TransactionHistoryJob>()
                   .As<IStartable>()
                   .AutoActivate()
                   .WithParameter("period", _settings.CurrentValue.TransactionHistoryJobPeriodSeconds * 1000)
                   .WithParameter("batchSize", _settings.CurrentValue.TransactionHistoryJobBatchSize)
                   .SingleInstance();

            builder.RegisterType<BroadcastInProgressJob>()
                   .As<IStartable>()
                   .AutoActivate()
                   .WithParameter("period", _settings.CurrentValue.BroadcastInProgressJobPeriodSeconds * 1000)
                   .WithParameter("batchSize", _settings.CurrentValue.BroadcastInProgressJobBatchSize)
                   .SingleInstance();

            builder.Populate(_services);
        }
    }
}
