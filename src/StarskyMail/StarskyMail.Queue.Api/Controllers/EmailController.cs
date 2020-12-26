using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        
        private const string ExchangeName = "starsky";
        private const string InvitationsQueueName = "starsky.invitations";
        private const string InvitationsRoutingKey = "invitations";

        public EmailController(ILogger<EmailController> logger, QueueConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("/invitations")]
        public ActionResult AddInvitationToQueue([FromBody] InvitationModel model)
        {
            var result = ValidateInvitation(model);
            if (result != null)
            {
                return result;
            }

            using var connection = _configuration.GetConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, true, false, null);
            channel.QueueDeclare(InvitationsQueueName, true, false, false, null);
            channel.QueueBind(InvitationsQueueName, ExchangeName, InvitationsRoutingKey);
            
            channel.BasicPublish(ExchangeName, InvitationsRoutingKey, null, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model)));
            
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