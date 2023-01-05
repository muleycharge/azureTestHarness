using Azure;
using Azure.Storage.Blobs;
using BO.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace BLL
{
    public class Receiver
    {
        private readonly ILogger<Receiver> _logger;
        private readonly BlobContainerClient _blobContainerClient;
        public Receiver(ILogger<Receiver> logger, IOptions<AzureTestHarnessOptions> options)
        {
            _logger = logger;
            _logger.LogInformation("Initializing {className}", nameof(Receiver));
            try
            {

                BlobServiceClient blobServiceClient = new BlobServiceClient(options.Value.BlobStorage.ConnectionString);
                _blobContainerClient = blobServiceClient.GetBlobContainerClient(options.Value.BlobStorage.TestContainerName);
                _blobContainerClient.CreateIfNotExists();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize {className} with options {@options}", nameof(Receiver), options.Value);
                throw;
            }
        }

        public async Task ReceiveTest(Guid messageId)
        {
            try
            {
                Response<bool> response = await _blobContainerClient.DeleteBlobIfExistsAsync(messageId.ToString()).ConfigureAwait(false);
                if (response.Value)
                {
                    _logger.LogInformation("Received message and deleted cooresponding blob with message id {messageId}", messageId);
                }
                else
                {
                    _logger.LogWarning("Recieved message but no cooresponding blob exists for message ID {messageId}, message likely a duplicate.", messageId);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured in {className}.{methodName}. Failed to delete blob for message ID {messageId}", nameof(Receiver), nameof(ReceiveTest), messageId);
                throw;
            }
        }
    }
}
