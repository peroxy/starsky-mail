using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace StarskyMail.Queue.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MailController : ControllerBase
    {
        private readonly ILogger<MailController> _logger;

        public MailController(ILogger<MailController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            return "Hello world!";
        }
    }
}