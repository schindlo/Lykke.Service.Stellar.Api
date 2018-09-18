using Autofac;
using Lykke.SettingsReader;
using Lykke.Job.Stellar.Api.Jobs;
using Lykke.Job.Stellar.Api.Settings;

namespace Lykke.Job.Stellar.Api.Modules
{
    public class StellarJobModule : Module
    {
        private readonly IReloadingManager<StellarJobSettings> _settings;

        public StellarJobModule(IReloadingManager<StellarJobSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WalletBalanceJob>()
                   .As<IStartable>()
                   .AutoActivate()
                   .WithParameter("period", _settings.CurrentValue.WalletBalanceJobPeriod)
                   .SingleInstance();

            builder.RegisterType<TransactionHistoryJob>()
                   .As<IStartable>()
                   .AutoActivate()
                   .WithParameter("period", _settings.CurrentValue.TransactionHistoryJobPeriod)
                   .SingleInstance();

            builder.RegisterType<BroadcastInProgressJob>()
                   .As<IStartable>()
                   .AutoActivate()
                   .WithParameter("period", _settings.CurrentValue.BroadcastInProgressJobPeriod)
                   .WithParameter("batchSize", _settings.CurrentValue.BroadcastInProgressJobBatchSize)
                   .SingleInstance();
        }
    }
}
