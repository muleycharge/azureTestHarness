using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using BO.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace BLL
{
    public class Sender
    {
        private readonly ILogger<Sender> _logger;
        protected readonly ServiceBusClient _client;
        protected Lazy<ServiceBusSender> _sender1;
        protected Lazy<ServiceBusSender> _sender2;
        private readonly BlobContainerClient _blobContainerClient;



        public Sender(ILogger<Sender> logger, IOptions<AzureTestHarnessOptions> options)
        {
            _logger = logger;
            _logger.LogInformation("Initializing {className}", nameof(Sender));
            try
            {


                _client = new ServiceBusClient(options.Value.ServiceBus.ConnectionString);
                _sender1 = new Lazy<ServiceBusSender>(() => _client.CreateSender(options.Value.ServiceBus.TestTopic1Name));
                _sender2 = new Lazy<ServiceBusSender>(() => _client.CreateSender(options.Value.ServiceBus.TestTopic2Name));
                BlobServiceClient blobServiceClient = new BlobServiceClient(options.Value.BlobStorage.ConnectionString);
                _blobContainerClient = blobServiceClient.GetBlobContainerClient(options.Value.BlobStorage.TestContainerName);
                _blobContainerClient.CreateIfNotExists();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize {className} with options {@options}", nameof(Sender), options.Value);
                throw;
            }
        }

        public Sender(string storageConnection, string storageContainerName, string busConnection, string topicName1)
        {
            _logger = Mock.Of<ILogger<Sender>>();
            _client = new ServiceBusClient(busConnection);
            _sender1 = new Lazy<ServiceBusSender>(() => _client.CreateSender(topicName1));
            _sender2 = new Lazy<ServiceBusSender>(() => throw new InvalidOperationException("Not initialized to be able to use Topic 2"));
            BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnection);
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(storageContainerName);
            _blobContainerClient.CreateIfNotExists();

        }

        public async Task SendTopic1(Guid messageId)
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
                await _sender1.Value.SendMessageAsync(message).ConfigureAwait(false);
                messageSent = true;
                _logger.LogInformation("Topic 1: Created message and cooresponding blob with message id {messageId}", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured in {className}.{methodName} sending message ID {messageId}. Blob Sent: {blobCreated}, Message Send: {messageSent}", nameof(Sender), nameof(SendTopic1), blobCreated, messageSent, messageId);
                throw;
            }
        }

        /// <summary>
        /// Pass thru message to next queue
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public async Task SendTopic2(Guid messageId)
        {
            try
            {
                ServiceBusMessage message = new ServiceBusMessage(JsonConvert.SerializeObject(messageId.ToString()));
                message.MessageId = messageId.ToString();
                message.ContentType = "application/json";
                await _sender2.Value.SendMessageAsync(message).ConfigureAwait(false);
                _logger.LogInformation("Topic 2: Created message and cooresponding blob with message id {messageId}", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured in {className}.{methodName} sending message ID {messageId}. ", nameof(Sender), nameof(SendTopic2), messageId);
                throw;
            }
        }
    }
}
