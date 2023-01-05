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

        [HttpGet("test1")]
        public async Task<IActionResult> Test1Get()
        {
            Guid messageId = Guid.NewGuid();
            await _sender.SendTest1Topic1(messageId).ConfigureAwait(false);

            return Ok(messageId);
        }

        [HttpGet("test1/{messageId}")]
        public async Task<IActionResult> Test1Get(Guid messageId)
        {
            await _sender.SendTest1Topic1(messageId).ConfigureAwait(false);

            return Ok(messageId);
        }

        [HttpGet("test2")]
        public async Task<IActionResult> Test2Get()
        {
            Guid messageId = Guid.NewGuid();
            await _sender.SendTest1Topic1(messageId).ConfigureAwait(false);

            return Ok(messageId);
        }

        [HttpGet("test2/{messageId}")]
        public async Task<IActionResult> Test2Get(Guid messageId)
        {
            await _sender.SendTest2Subscription1(messageId).ConfigureAwait(false);

            return Ok(messageId);
        }
    }
}
