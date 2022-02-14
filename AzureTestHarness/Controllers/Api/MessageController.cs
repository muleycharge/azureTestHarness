using BLL;
using Microsoft.AspNetCore.Mvc;

namespace AzureTestHarness.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly ILogger<MessageController> _logger;

        private readonly Sender _sender;

        public MessageController(ILogger<MessageController> logger, Sender sender)
        {
            _logger = logger;
            _sender = sender;
        }

        [HttpGet()]
        public async Task<IActionResult> Get()
        {
            Guid messageId = Guid.NewGuid();
            await _sender.Send(messageId).ConfigureAwait(false);

            return Ok(messageId);
        }

        [HttpGet("{messageId}")]
        public async Task<IActionResult> Get(Guid messageId)
        {
            await _sender.Send(messageId).ConfigureAwait(false);

            return Ok(messageId);
        }
    }
}
