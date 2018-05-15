using System;
using Autofac;
using Common.Log;
using JetBrains.Annotations;

namespace Lykke.Service.Stellar.Api.Client
{
    public static class AutofacExtension
    {
        public static void RegisterStellarApiClient(this ContainerBuilder builder, string serviceUrl, ILog log)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (serviceUrl == null) throw new ArgumentNullException(nameof(serviceUrl));
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (string.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));

            builder.RegisterType<StellarApiClient>()
                .WithParameter("serviceUrl", serviceUrl)
                .As<IStellarApiClient>()
                .SingleInstance();
        }

        [UsedImplicitly]
        public static void RegisterStellarApiClient(this ContainerBuilder builder, StellarApiServiceClientSettings settings, ILog log)
        {
            builder.RegisterStellarApiClient(settings?.ServiceUrl, log);
        }
    }
}
