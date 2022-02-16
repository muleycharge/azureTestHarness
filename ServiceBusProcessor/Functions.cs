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
        private readonly Sender _sender;

        public Functions(ILogger<Functions> logger, Receiver receiver, Sender sender)
        {
            _logger = logger;
            _receiver = receiver;
            _sender = sender;
        }

        public async Task Receive1([ServiceBusTrigger("%AzureTestHarness:ServiceBus:TestTopic1Name%", "%AzureTestHarness:ServiceBus:TestSubscriptionName%", Connection = "AzureTestHarness:ServiceBus:ConnectionString")] Guid messageId,
            ILogger logger)
        {
            try
            {
                // Simulate process intensive task
                await Task.Delay(3000);
                await _sender.SendTopic2(messageId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to process message {e.Message}");
                _logger.LogError(e, "Failed to process message ID {messageId}", messageId);
            }

            logger.LogInformation($"Completed processing message ID {messageId}");
        }

        public async Task Receive2([ServiceBusTrigger("%AzureTestHarness:ServiceBus:TestTopic2Name%", "%AzureTestHarness:ServiceBus:TestSubscriptionName%", Connection = "AzureTestHarness:ServiceBus:ConnectionString")] Guid messageId,
            ILogger logger)
        {
            try
            {
                // Simulate process intensive task
                await Task.Delay(3000);
                // Indicate that message was relayed correctly by deleting corresponding blob
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
