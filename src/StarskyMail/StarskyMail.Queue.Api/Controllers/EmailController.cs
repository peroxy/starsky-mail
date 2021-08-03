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
using StarskyMail.Queue.Extensions;
using StarskyMail.Queue.Models;
using StarskyMail.Queue.Settings;
using IModel = RabbitMQ.Client.IModel;

namespace StarskyMail.Queue.Api.Controllers
{
    [ApiController]
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
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        public ActionResult AddInvitationToQueue([FromBody] InvitationsMailModel model)
        {
            (bool success, string reason) = model.Validate();
            if (!success)
            {
                _logger.LogWarning($"Bad request due to: {reason}");
                return BadRequest(reason);
            }

            var connectionInfo = GetCachedConnection();

            connectionInfo.Channel.BasicPublish(
                _rabbitSettings.ExchangeName,
                _rabbitSettings.InvitationsRoutingKey,
                null,
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model)));
            
            return Accepted();
        }
        
        [HttpPost]
        [Route("/schedule-notifications")]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        public ActionResult AddScheduleNotifyToQueue([FromBody] ScheduleNotifyModel model)
        {
            (bool success, string reason) = model.Validate();
            if (!success)
            {
                _logger.LogWarning($"Bad request due to: {reason}");
                return BadRequest(reason);
            }

            var connectionInfo = GetCachedConnection();

            connectionInfo.Channel.BasicPublish(
                _rabbitSettings.ExchangeName,
                _rabbitSettings.ScheduleNotifyRoutingKey,
                null,
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model)));
            
            return Accepted();
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
                var model = connection.CreatePersistentChannel();
                return (connection, model);
            });
        }
    }
}