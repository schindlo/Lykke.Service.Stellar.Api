using Autofac;
using Common.Log;
using Lykke.SettingsReader;
using Lykke.Service.Stellar.Api.AzureRepositories.Transaction;
using Lykke.Service.Stellar.Api.AzureRepositories.Balance;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Service.Stellar.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Stellar.Api.AzureRepositories.Observation;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Lykke.Service.Stellar.Api.Core.Domain;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Modules
{
    public class RepositoryModule : Module
    {
        private readonly IReloadingManager<StellarApiSettings> _settings;
        private readonly ILog _log;

        public RepositoryModule(IReloadingManager<StellarApiSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
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

            builder.RegisterType<KeyValueStoreRepository>()
                    .As<IKeyValueStoreRepository>()
                    .WithParameter(TypedParameter.From(dataConnStringManager));
        }
    }
}
