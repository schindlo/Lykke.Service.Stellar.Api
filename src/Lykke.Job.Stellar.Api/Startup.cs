using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Metamodel;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Lykke.Common;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Common.Chaos;
using Lykke.Common.Log;
using Lykke.Job.Stellar.Api.Modules;
using Lykke.Job.Stellar.Api.Settings;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeSlack;
using Lykke.Service.Stellar.Api.AzureRepositories.Modules;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Services.Modules;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.Stellar.Api
{
    public class Startup
    {
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; private set; }
        public IConfigurationRoot Configuration { get; }
        public ILog Log { get; private set; }
        public IHealthNotifier HealthNotifier { get; set; }

        [UsedImplicitly]
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddHttpClient();
                services.AddMvc()
                    .AddJsonOptions(options =>
                    {
                        options.SerializerSettings.ContractResolver =
                            new Newtonsoft.Json.Serialization.DefaultContractResolver();
                    });

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration("v1", "JobStellarApi API");
                });

                EntityMetamodel.Configure(new AnnotationsBasedMetamodelProvider());

                var builder = new ContainerBuilder();
                var appSettings = Configuration.LoadSettings<AppSettings>(options =>
                {
                    options.SetConnString(x => x.SlackNotifications.AzureQueue.ConnectionString);
                    options.SetQueueName(x => x.SlackNotifications.AzureQueue.QueueName);
                    options.SenderName = AppEnvironment.Name;
                });

                services.AddSingleton<Lykke.Service.Stellar.Api.Core.Settings.AppSettings>(appSettings.CurrentValue);

                services.AddLykkeLogging(
                    appSettings.ConnectionString(x => x.StellarApiService.Db.LogsConnString),
                    "JobStellarApiLog",
                    appSettings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                    appSettings.CurrentValue.SlackNotifications.AzureQueue.QueueName,
                    logBuilder =>
                    {
                        logBuilder.AddAdditionalSlackChannel("BlockChainIntegration", options =>
                        {
                            options.MinLogLevel = Microsoft.Extensions.Logging.LogLevel.Information; // Let it be explicit
                        });

                        logBuilder.AddAdditionalSlackChannel("BlockChainIntegrationImportantMessages", options =>
                        {
                            options.MinLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
                        });
                    }
                    );

                builder.RegisterChaosKitty(appSettings.CurrentValue.StellarApiService.ChaosKitty);
                builder.RegisterModule(new StellarJobModule(appSettings.Nested(x => x.StellarApiJob)));
                builder.RegisterModule(new RepositoryModule(appSettings.Nested(x => x.StellarApiService)));
                builder.RegisterModule(new ServiceModule(appSettings.Nested(x => x.StellarApiService)));
                builder.Populate(services);

                ApplicationContainer = builder.Build();

                Log = ApplicationContainer.Resolve<ILogFactory>().CreateLog(this);
                HealthNotifier = ApplicationContainer.Resolve<IHealthNotifier>();

                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            try
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseLykkeForwardedHeaders();
                app.UseLykkeMiddleware(ex => new { Message = "Technical problem" });

                app.UseMvc();
                app.UseSwagger(c =>
                {
                    c.PreSerializeFilters.Add((swagger, httpReq) => swagger.Host = httpReq.Host.Value);
                });
                app.UseSwaggerUI(x =>
                {
                    x.RoutePrefix = "swagger/ui";
                    x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                });
                app.UseStaticFiles();

                appLifetime.ApplicationStarted.Register(() => StartApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopping.Register(() => StopApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopped.Register(CleanUp);
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }

        private async Task StartApplication()
        {
            try
            {
                // NOTE: Service not yet recieve and process requests here

                await ApplicationContainer.Resolve<IStartupManager>().StartAsync();

                HealthNotifier?.Notify("Started");
            }
            catch (Exception ex)
            {
                Log.Critical(ex);
                throw;
            }
        }

        private async Task StopApplication()
        {
            try
            {
                // NOTE: Service still can recieve and process requests here, so take care about it if you add logic here.

                await ApplicationContainer.Resolve<IShutdownManager>().StopAsync();
            }
            catch (Exception ex)
            {
                Log.Critical(ex);
                throw;
            }
        }

        private void CleanUp()
        {
            try
            {
                // NOTE: Service can't recieve and process requests here, so you can destroy all resources

                HealthNotifier?.Notify("Terminating");

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                // ReSharper disable once InvertIf
                Log?.Critical(ex);
                (Log as IDisposable)?.Dispose();
                throw;
            }
        }
    }
}
