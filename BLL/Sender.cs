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
        private const string Test2ActionKey = "step";
        private readonly ILogger<Sender> _logger;
        protected readonly ServiceBusClient _client;
        protected Lazy<ServiceBusSender> _senderTest1Topic1;
        protected Lazy<ServiceBusSender> _senderTest1Topic2;
        protected Lazy<ServiceBusSender> _senderTest2Topic;
        private readonly BlobContainerClient _blobContainerClient;



        public Sender(ILogger<Sender> logger, IOptions<AzureTestHarnessOptions> options)
        {
            _logger = logger;
            _logger.LogInformation("Initializing {className}", nameof(Sender));
            try
            {


                _client = new ServiceBusClient(options.Value.ServiceBus.ConnectionString);
                _senderTest1Topic1 = new Lazy<ServiceBusSender>(() => _client.CreateSender(options.Value.ServiceBus.Test1Topic1Name));
                _senderTest1Topic2 = new Lazy<ServiceBusSender>(() => _client.CreateSender(options.Value.ServiceBus.Test1Topic2Name));
                _senderTest2Topic = new Lazy<ServiceBusSender>(() => _client.CreateSender(options.Value.ServiceBus.Test2TopicName));
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

        public Sender(string storageConnection, string storageContainerName, string busConnection, string test1TopicName, string test2TopicName)
        {
            _logger = Mock.Of<ILogger<Sender>>();
            _client = new ServiceBusClient(busConnection);
            _senderTest1Topic1 = new Lazy<ServiceBusSender>(() => _client.CreateSender(test1TopicName));
            _senderTest1Topic2 = new Lazy<ServiceBusSender>(() => throw new InvalidOperationException("Not initialized to be able to use Topic 2"));
            _senderTest2Topic = new Lazy<ServiceBusSender>(() => _client.CreateSender(test2TopicName));
            BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnection);
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(storageContainerName);
            _blobContainerClient.CreateIfNotExists();

        }

        public async Task SendTest1Topic1(Guid messageId)
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
                await _senderTest1Topic1.Value.SendMessageAsync(message).ConfigureAwait(false);
                messageSent = true;
                _logger.LogInformation("Topic 1: Created message and cooresponding blob with message id {messageId}", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured in {className}.{methodName} sending message ID {messageId}. Blob Sent: {blobCreated}, Message Send: {messageSent}", nameof(Sender), nameof(SendTest1Topic1), blobCreated, messageSent, messageId);
                throw;
            }
        }

        /// <summary>
        /// Pass thru message to next queue
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public async Task SendTest1Topic2(Guid messageId)
        {
            try
            {
                ServiceBusMessage message = new ServiceBusMessage(JsonConvert.SerializeObject(messageId.ToString()));
                message.MessageId = messageId.ToString();
                message.ContentType = "application/json";
                await _senderTest1Topic2.Value.SendMessageAsync(message).ConfigureAwait(false);
                _logger.LogInformation("Topic 2: Created message and cooresponding blob with message id {messageId}", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured in {className}.{methodName} sending message ID {messageId}. ", nameof(Sender), nameof(SendTest1Topic2), messageId);
                throw;
            }
        }

        /// <summary>
        /// Pass thru message to next queue
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public async Task SendTest2Subscription1(Guid messageId)
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
                message.ApplicationProperties.Add(Test2ActionKey, 1);
                await _senderTest2Topic.Value.SendMessageAsync(message).ConfigureAwait(false);
                messageSent = true;
                _logger.LogInformation("Topic 2: Created message with property step=1 and cooresponding blob with message id {messageId}", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured in {className}.{methodName} sending message ID {messageId}. Blob Sent: {blobCreated}, Message Send: {messageSent}", nameof(Sender), nameof(SendTest2Subscription1), blobCreated, messageSent, messageId);
                throw;
            }
        }

        /// <summary>
        /// Pass thru message to next queue
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public async Task SendTest2Subscription2(Guid messageId)
        {
            try
            {
                ServiceBusMessage message = new ServiceBusMessage(JsonConvert.SerializeObject(messageId.ToString()));
                message.MessageId = messageId.ToString();
                message.ContentType = "application/json";
                message.ApplicationProperties.Add(Test2ActionKey, 2);
                await _senderTest2Topic.Value.SendMessageAsync(message).ConfigureAwait(false);
                _logger.LogInformation("Topic 2: Created message with property step=2 and cooresponding blob with message id {messageId}", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured in {className}.{methodName} sending message ID {messageId}. ", nameof(Sender), nameof(SendTest2Subscription2), messageId);
                throw;
            }
        }
    }
}
