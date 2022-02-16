using CommandLine;

namespace ServiceBusSender
{
    public class Options
    {
        [Option('m', "message-count", Default = 1, HelpText = "Number of test messages to add to the queue.")]
        public int MessageCount { get; set; }

        [Option('b', "service-bus-conn", HelpText = "Service Bus connection string", Required = true)]
        public string ServiceBusConnection { get; set; }

        [Option('t', "topic-name", HelpText = "Where test messages are sent", Required = true)]
        public string TopicName { get; set; }

        [Option('s', "storage-conn", HelpText = "Storage connection string", Required = true)]
        public string StorageConnection { get; set; }

        [Option('c', "storage-container", HelpText = "Storage container name", Required = true)]
        public string StorageContainer { get; set; }

        [Option('i', "insights-key", HelpText = "Application Insights instrumentation key", Required = true)]
        public string ApplicationInsightsKey { get; set; }
    }
}
