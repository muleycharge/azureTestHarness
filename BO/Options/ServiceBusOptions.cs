namespace BO.Options
{
    public class ServiceBusOptions
    {
        public const string ServiceBus = "ServiceBus";

        public string ConnectionString { get; set; }

        public string TestTopicName { get; set; }

        public string TestSubscriptionName { get; set; }
    }
}
