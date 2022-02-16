using BLL;
using CommandLine;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBusSender
{
    internal class Program
    {
        private static CancellationTokenSource cancelToken = new CancellationTokenSource();

        private const int serviceBusBatchSize = 50;

        static async Task Main(string[] args)
        {
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
            {
                e.Cancel = true;
                cancelToken.Cancel();
            };

            (await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(SendMessages).ConfigureAwait(false))
                .WithNotParsed(errors =>
                {
                    var jsonString = JsonConvert.SerializeObject(
                        args, Formatting.Indented, new JsonConverter[] { new StringEnumConverter() });
                    Console.WriteLine(jsonString);
                    foreach (Error error in errors)
                    {
                        Console.WriteLine(error);
                    }
                });

        }

        private static async Task SendMessages(Options options)
        {
            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = options.ApplicationInsightsKey;
            TelemetryClient telemetryClient = new TelemetryClient(configuration);

            try
            {
                Sender sender = new Sender(options.StorageConnection, options.StorageContainer, options.ServiceBusConnection, options.TopicName);
                Task[] tasks = new Task[options.MessageCount];
                for (int i = 0; i < options.MessageCount; i++)
                {
                    Guid messageId = Guid.NewGuid();
                    tasks[i] = sender.SendTopic1(messageId)
                        .ContinueWith(t =>
                        {
                            Console.WriteLine(messageId);
                            return t;
                        });
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                telemetryClient.TrackException(ex, properties: new Dictionary<string, string>() { { "Message", "Failed to send batch" } });

                throw;
            }

        }
    }
}
