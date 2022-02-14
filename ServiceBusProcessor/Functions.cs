using BLL;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ServiceBusProcessor
{

    public class Functions
    {
        private readonly ILogger<Functions> _logger;
        private readonly Receiver _receiver;
        public Functions(ILogger<Functions> logger, Receiver receiver)
        {
            _logger = logger;
            _receiver = receiver;
        }

        public async Task Receive([ServiceBusTrigger("%AzureTestHarness:ServiceBus:TestTopicName%", "%AzureTestHarness:ServiceBus:TestSubscriptionName%", Connection = "AzureTestHarness:ServiceBus:ConnectionString")] Guid messageId,
            ILogger logger)
        {
            try
            {
                // Simulate process intensive task
                await Task.Delay(3000);
                await _receiver.Receive(messageId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to process message {e.Message}");
                _logger.LogError(e, "Failed to process message ID {messageId}", messageId);
            }

            logger.LogInformation($"Completed processing message ID {messageId}");
        }

    }
}
