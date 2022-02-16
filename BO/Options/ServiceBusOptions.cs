namespace BO.Options
{
    public class ServiceBusOptions
    {
        public const string ServiceBus = "ServiceBus";

        public string ConnectionString { get; set; }

        public string TestTopic1Name { get; set; }

        public string TestSubscription1Name { get; set; }

        public string TestTopic2Name { get; set; }

        public string TestSubscription2Name { get; set; }
    }
}
