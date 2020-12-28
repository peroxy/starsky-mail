using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using StarskyMail.Queue.Api.Models;

namespace StarskyMail.Queue.Api.Controllers
{
    [ApiController]
    [Route("/emails")]
    public class EmailController : ControllerBase
    {
        private readonly ILogger<EmailController> _logger;
        private readonly QueueConfiguration _configuration;
        private readonly RabbitMQSettings _rabbitSettings;
        
        public EmailController(ILogger<EmailController> logger, QueueConfiguration configuration, IOptions<RabbitMQSettings> rabbitSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _rabbitSettings = rabbitSettings.Value;
        }

        [HttpPost]
        [Route("/invitations")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public ActionResult AddInvitationToQueue([FromBody] InvitationModel model)
        {
            var result = ValidateInvitation(model);
            if (result != null)
            {
                return result;
            }

            using var connection = _configuration.GetConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(_rabbitSettings.ExchangeName, ExchangeType.Direct, true, false, null);
            channel.QueueDeclare(_rabbitSettings.InvitationsQueueName, true, false, false, null);
            channel.QueueBind(_rabbitSettings.InvitationsQueueName, _rabbitSettings.ExchangeName, _rabbitSettings.InvitationsRoutingKey);

            channel.BasicPublish(
                _rabbitSettings.ExchangeName,
                _rabbitSettings.InvitationsRoutingKey,
                null,
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model)));
            
            channel.Close();

            return Ok();
        }

        private ActionResult ValidateInvitation(InvitationModel model)
        {
            if (string.IsNullOrWhiteSpace(model.EmployeeEmail) || !model.EmployeeEmail.Contains("@"))
            {
                return BadRequest("Email is invalid!");
            }

            if (string.IsNullOrWhiteSpace(model.EmployeeName))
            {
                return BadRequest("Employee name is invalid!");
            }

            if (string.IsNullOrWhiteSpace(model.ManagerName))
            {
                return BadRequest("Manager name is invalid!");
            }

            if (string.IsNullOrWhiteSpace(model.RegisterUrl))
            {
                return BadRequest("Register URL is invalid!");
            }

            return null;
        }
    }
}