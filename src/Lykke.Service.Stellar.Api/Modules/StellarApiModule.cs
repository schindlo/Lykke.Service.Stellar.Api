using Autofac;
using Common.Log;

namespace Lykke.Service.Stellar.Api.Modules
{
    public class StellarApiModule : Module
    {
        private readonly ILog _log;

        public StellarApiModule(ILog log)
        {
            _log = log;
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
        }
    }
}
