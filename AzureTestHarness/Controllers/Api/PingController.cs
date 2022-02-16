using Microsoft.AspNetCore.Mvc;

namespace AzureTestHarness.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PingController : ControllerBase
    {

        [HttpGet]
        public IActionResult Get()
        {
            return NoContent();
        }

    }
}
