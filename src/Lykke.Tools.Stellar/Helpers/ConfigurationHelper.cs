using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Common;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Service.Assets.Client;
using Lykke.Service.Stellar.Api.AzureRepositories.Modules;
using Lykke.Service.Stellar.Api.Core.Settings;
using Lykke.Service.Stellar.Api.Modules;
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
        public (IContainer resolver, ILog logToConsole) GetResolver(string settingsUrl)
        {

            ContainerBuilder containerBuilder = new ContainerBuilder();
            IServiceCollection collection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

            var appSettings = GetCurrentSettingsFromUrl(settingsUrl);

            var consoleLogger = LogFactory.Create();
            consoleLogger.AddConsole(options => { options.IncludeScopes = true; });
            var log = consoleLogger.CreateLog(this);
            collection.AddSingleton<ILog>(log);
            containerBuilder.RegisterModule(new StellarApiModule());
            containerBuilder.RegisterModule(new RepositoryModule(appSettings.Nested(x => x.StellarApiService)));
            containerBuilder.RegisterModule(new ServiceModule(appSettings.Nested(x => x.StellarApiService)));
            containerBuilder.Populate(collection);

            var resolver = containerBuilder.Build();
            return (resolver, log);
        }

        public IReloadingManager<AppSettings> GetCurrentSettingsFromUrl(string settingsUrl)
        {
            var keyValuePair = new KeyValuePair<string, string>[1]
            {
                new KeyValuePair<string, string>("SettingsUrl", settingsUrl)
            };

            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            builder.AddInMemoryCollection(keyValuePair);
            Configuration = builder.Build();
            var appSettings = Configuration.LoadSettings<AppSettings>(options =>
            {
                options.SetConnString(x => x.SlackNotifications.AzureQueue.ConnectionString);
                options.SetQueueName(x => x.SlackNotifications.AzureQueue.QueueName);
                options.SenderName = AppEnvironment.Name;
            });

            return appSettings;
        }

        public IConfigurationRoot Configuration { get; set; }
    }
}
