using System.Collections.Generic;
using System.Net.Http;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Common;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.Assets.Client;
using Lykke.Service.Stellar.Api.AzureRepositories.Modules;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Settings;
using Lykke.Service.Stellar.Api.Modules;
using Lykke.Service.Stellar.Api.Services.Horizon;
using Lykke.Service.Stellar.Api.Services.Modules;
using Lykke.SettingsReader;
using Lykke.Tools.Erc20Exporter.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Tools.Stellar.Helpers
{
    public class ConfigurationHelper : IConfigurationHelper
    {
        public (IContainer resolver, ILog logToConsole) GetResolver(string horizonUrl, string passPhrase)
        {
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IServiceCollection collection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

            var consoleLogger = LogFactory.Create();
            consoleLogger.AddConsole(options => { options.IncludeScopes = true; });
            var log = consoleLogger.CreateLog(this);
            collection.AddSingleton<ILog>(log);
            collection.AddHttpClient();
            containerBuilder.RegisterType<HorizonService>()
                .As<IHorizonService>()
                .WithParameter("network", passPhrase)
                .WithParameter("horizonUrl", horizonUrl)
                .SingleInstance();
            containerBuilder.Populate(collection);

            var resolver = containerBuilder.Build();
            return (resolver, log);
        }
    }
}
