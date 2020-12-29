using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using StarskyMail.Queue.Models;

namespace StarskyMail.Queue.Api.Controllers
{
    [ApiController]
    [Route("/emails")]
    public class EmailController : ControllerBase
    {
        private readonly ILogger<EmailController> _logger;
        private readonly QueueConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly RabbitMQSettings _rabbitSettings;
        
        private const string ConnectionCacheKey = "rabbitMqConnection";
        
        public EmailController(ILogger<EmailController> logger, QueueConfiguration configuration, IOptions<RabbitMQSettings> rabbitSettings, IMemoryCache cache)
        {
            _logger = logger;
            _configuration = configuration;
            _cache = cache;
            _rabbitSettings = rabbitSettings.Value;
        }

        [HttpPost]
        [Route("/invitations")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public ActionResult AddInvitationToQueue([FromBody] InvitationModel model)
        {
            var result = ValidateInvitation(model);
            if (result != null)
            {
                _logger.LogWarning($"Bad request due to: {result.Value}");
                return result;
            }

            var connectionInfo = GetCachedConnection();

            connectionInfo.Channel.BasicPublish(
                _rabbitSettings.ExchangeName,
                _rabbitSettings.InvitationsRoutingKey,
                null,
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model)));
            
            return Ok();
        }

        private (IConnection Connection, IModel Channel) GetCachedConnection()
        {
            return _cache.GetOrCreate(ConnectionCacheKey, entry =>
            {
                entry.RegisterPostEvictionCallback((_, value, _, _) =>
                {
                    var (conn, mod) = (ValueTuple<IConnection, IModel>) value;
                    try
                    {
                        mod.Close();
                        conn.Close();
                    }
                    finally
                    {
                        mod.Dispose();
                        conn.Dispose();
                    }
                    
                    _logger.LogInformation("RabbitMQ connection has been successfully closed after it has expired in cache.");
                });

                entry.SlidingExpiration = TimeSpan.FromMinutes(1);
                var connection = _configuration.GetConnection();
                var model = connection.CreateModel();
                return (connection, model);
            });
        }

        private BadRequestObjectResult ValidateInvitation(InvitationModel model)
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