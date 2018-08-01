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
            builder.RegisterInstance(_log)
                   .As<ILog>()
                   .SingleInstance();
        }
    }
}
