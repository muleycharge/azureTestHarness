using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Storage.Blobs;
using BO.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace BLL
{
    public class Sender
    {
        private readonly ILogger<Sender> _logger;
        protected readonly ServiceBusAdministrationClient _serviceBusAdmin;
        protected readonly ServiceBusClient _client;
        protected ServiceBusSender _sender;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _blobContainerClient;
        public Sender(ILogger<Sender> logger, IOptions<AzureTestHarnessOptions> options)
        {
            _logger = logger;
            _logger.LogInformation("Initializing {className}", nameof(Sender));
            try
            {
                _serviceBusAdmin = new ServiceBusAdministrationClient(options.Value.ServiceBus.ConnectionString);
                Task.Factory.StartNew(async () =>
                {
                    if (!await _serviceBusAdmin.TopicExistsAsync(options.Value.ServiceBus.TestTopicName))
                    {
                        _logger.LogInformation("Creating topic {topicName}", options.Value.ServiceBus.TestTopicName);
                        await _serviceBusAdmin.CreateTopicAsync(options.Value.ServiceBus.TestTopicName).ConfigureAwait(false);
                    }
                    if (!await _serviceBusAdmin.SubscriptionExistsAsync(options.Value.ServiceBus.TestTopicName, options.Value.ServiceBus.TestSubscriptionName))
                    {
                        _logger.LogInformation("Creating subscription {topicName}/{subscriptionName}", options.Value.ServiceBus.TestTopicName, options.Value.ServiceBus.TestSubscriptionName);
                        await _serviceBusAdmin.CreateSubscriptionAsync(options.Value.ServiceBus.TestTopicName, options.Value.ServiceBus.TestSubscriptionName).ConfigureAwait(false);
                    }
                }).Wait();


                _client = new ServiceBusClient(options.Value.ServiceBus.ConnectionString);
                _sender = _client.CreateSender(options.Value.ServiceBus.TestTopicName);
                _blobServiceClient = new BlobServiceClient(options.Value.BlobStorage.ConnectionString);
                _blobContainerClient = _blobServiceClient.GetBlobContainerClient(options.Value.BlobStorage.TestContainerName);
                _blobContainerClient.CreateIfNotExists();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to initialize {className} with options {@options}", nameof(Sender), options.Value);
                throw;
            }

        }
        public async Task Send(Guid messageId)
        {
            bool blobCreated = false;
            bool messageSent = false;
            try
            {
                await _blobContainerClient.UploadBlobAsync(messageId.ToString(), BinaryData.FromString(messageId.ToString())).ConfigureAwait(false);
                blobCreated = true;
                ServiceBusMessage message = new ServiceBusMessage(JsonConvert.SerializeObject(messageId.ToString()));
                message.MessageId = messageId.ToString();
                message.ContentType = "application/json";
                await _sender.SendMessageAsync(message).ConfigureAwait(false);
                messageSent = true;
                _logger.LogInformation("Created message and cooresponding blob with message id {messageId}", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured in {className}.{methodName}. Blob Sent: {blobCreated}, Message Send: {messageSent}", nameof(Sender), nameof(Send), blobCreated, messageSent);
                throw;
            }
        }
    }
}
