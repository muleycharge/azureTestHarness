using BLL;
using BO.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ServiceBusProcessor
{
    public static class Program
    {

        // <summary>
        /// Main App Lifecycle Logger Instance
        /// </summary>
        private static readonly NLog.Logger _logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();


        /// <summary>
        /// config values
        /// </summary>
        private static IConfiguration _configuration;
        public static async Task Main()
        {
            HostBuilder builder = new HostBuilder();
            builder.ConfigureAppConfiguration((context, config) => { ConfigureAppConfig(config); });

            builder.ConfigureLogging(ConfigureLogging);
            builder.ConfigureServices(services =>
            {

                services.Configure<AzureTestHarnessOptions>(_configuration.GetSection(AzureTestHarnessOptions.AzureTestHarness));
                services.AddSingleton<Sender>();
                services.AddSingleton<Receiver>();
                services.AddLogging();
            });
            builder.ConfigureWebJobs(b =>
            {
                b.AddAzureStorageCoreServices();

                b.AddServiceBus(q =>
                {
                    q.MaxMessageBatchSize = 20;
                    q.AutoCompleteMessages = true;
                });
            });

            IHost host = builder.Build();
            using (host)
            {
                await host.RunAsync();
            }
        }

        private static void ConfigureAppConfig(IConfigurationBuilder config)
        {
            try
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile("appsettings.json.user", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();

                _configuration = config.Build();

            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to load configuration.");
                throw;
            }
        }

        /// <summary>
        /// Set up logging 
        /// </summary>
        /// </summary>
        /// <param name="loggingBuilder">used to configure logging provider</param>
        private static void ConfigureLogging(ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            string nlogConfigFile = "nlog.config";
            loggingBuilder.AddNLog(nlogConfigFile);
        }
    }
}
