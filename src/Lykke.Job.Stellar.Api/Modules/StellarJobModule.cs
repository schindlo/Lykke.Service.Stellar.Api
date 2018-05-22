using Autofac;
using Common.Log;
using Lykke.SettingsReader;
using Lykke.Job.Stellar.Api.Jobs;
using Lykke.Job.Stellar.Api.Settings;

namespace Lykke.Job.Stellar.Api.Modules
{
    public class StellarJobModule : Module
    {
        private readonly IReloadingManager<StellarJobSettings> _settings;
        private readonly ILog _log;

        public StellarJobModule(IReloadingManager<StellarJobSettings> settings,
                                ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                   .As<ILog>()
                   .SingleInstance();

            builder.RegisterType<WalletBalanceJob>()
                   .As<IStartable>()
                   .AutoActivate()
                   .WithParameter("period", _settings.CurrentValue.WalletBalanceJobPeriod.TotalMilliseconds)
                   .SingleInstance();

            builder.RegisterType<TransactionHistoryJob>()
                   .As<IStartable>()
                   .AutoActivate()
                   .WithParameter("period", _settings.CurrentValue.TransactionHistoryJobPeriod.TotalMilliseconds)
                   .SingleInstance();

            builder.RegisterType<BroadcastInProgressJob>()
                   .As<IStartable>()
                   .AutoActivate()
                   .WithParameter("period", _settings.CurrentValue.BroadcastInProgressJobPeriod.TotalMilliseconds)
                   .WithParameter("batchSize", _settings.CurrentValue.BroadcastInProgressJobBatchSize)
                   .SingleInstance();
        }
    }
}
