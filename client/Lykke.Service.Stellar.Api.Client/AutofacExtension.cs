using System;
using Autofac;
using Common.Log;

namespace Lykke.Service.Stellar.Api.Client
{
    public static class AutofacExtension
    {
        public static void RegisterStellar.ApiClient(this ContainerBuilder builder, string serviceUrl, ILog log)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (serviceUrl == null) throw new ArgumentNullException(nameof(serviceUrl));
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (string.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));

            builder.RegisterType<Stellar.ApiClient>()
                .WithParameter("serviceUrl", serviceUrl)
                .As<IStellar.ApiClient>()
                .SingleInstance();
        }

        public static void RegisterStellar.ApiClient(this ContainerBuilder builder, Stellar.ApiServiceClientSettings settings, ILog log)
        {
            builder.RegisterStellar.ApiClient(settings?.ServiceUrl, log);
        }
    }
}
