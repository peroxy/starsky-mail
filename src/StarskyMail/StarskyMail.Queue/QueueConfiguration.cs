using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using StarskyMail.Queue.Extensions;
using StarskyMail.Queue.Settings;

namespace StarskyMail.Queue
{
    public class QueueConfiguration
    {
        private readonly ILogger<QueueConfiguration> _logger;
        private readonly ConnectionFactory _factory;
        private readonly RabbitMQSettings _rabbitSettings;
        
        private const string DeadLetterExchangeArgument = "x-dead-letter-exchange";

        public QueueConfiguration(IOptions<RabbitMQSettings> configuration, ILogger<QueueConfiguration> logger)
        {
            _logger = logger;
            _rabbitSettings = configuration.Value;
            
            _factory = new ConnectionFactory
            {
                UserName = _rabbitSettings.Username,
                Password = _rabbitSettings.Password,
                HostName = _rabbitSettings.Hostname,
                Port = _rabbitSettings.Port
            };
        }
        
        public IConnection GetConnection()
        {
            return _factory.CreateConnection();
        }

        private (bool reachable, Exception brokerUnreachableException) IsRabbitReachable()
        {
            try
            {
                using var connection = GetConnection();
                connection.Close();
                return (true, null);
            }
            catch (BrokerUnreachableException e)
            {
                return (false, e);
            }
        }

        public void TryConnect(int retryCount)
        {
            for (int i = 0; i < retryCount; i++)
            {
                (bool reachable, var brokerUnreachableException) = IsRabbitReachable();
                if (reachable)
                {
                    _logger.LogInformation($"RabbitMQ broker is reachable at {_factory.HostName}:{_factory.Port} - success");
                    break;
                }

                if (i == retryCount - 1)
                {
                    _logger.LogCritical($"RabbitMQ broker is unreachable at {_factory.HostName}:{_factory.Port}, can't establish a connection, will stop retrying");
                    throw brokerUnreachableException;
                }

                _logger.LogWarning($"[#{i}] RabbitMQ broker is unreachable at at {_factory.HostName}:{_factory.Port} - retrying in 5 seconds..");
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        /// <summary>
        /// Code-first approach to RabbitMQ queues and exchanges - will create everything from scratch if it does not exist, otherwise it just checks everything is OK.
        /// </summary>
        public (IConnection connection, IModel channel) CreateRabbitInfrastructure(bool closeConnection = false)
        {
            var connection = GetConnection();
            var channel = connection.CreatePersistentChannel();

            var deadLetterExchange = $"dead.letter.{_rabbitSettings.ExchangeName}";
            var invitationsDeadLetter = $"dead.letter.{_rabbitSettings.InvitationsQueueName}";
            var scheduleNotifyDeadLetter = $"dead.letter.{_rabbitSettings.ScheduleNotifyQueueName}";
            
            channel.ExchangeDeclare(deadLetterExchange, ExchangeType.Fanout, true);
            
            channel.QueueDeclare(invitationsDeadLetter, true, false, false);
            channel.QueueBind(invitationsDeadLetter, deadLetterExchange, "dead.letter");
            
            channel.QueueDeclare(scheduleNotifyDeadLetter, true, false, false);
            channel.QueueBind(scheduleNotifyDeadLetter, deadLetterExchange, "dead.letter");
            
            channel.ExchangeDeclare(_rabbitSettings.ExchangeName, ExchangeType.Direct, true);
            
            channel.QueueDeclare(_rabbitSettings.InvitationsQueueName,
                true,
                false,
                false,
                new Dictionary<string, object> {{DeadLetterExchangeArgument, deadLetterExchange}});
            channel.QueueBind(_rabbitSettings.InvitationsQueueName, _rabbitSettings.ExchangeName, _rabbitSettings.InvitationsRoutingKey);
            
            channel.QueueDeclare(_rabbitSettings.ScheduleNotifyQueueName,
                true,
                false,
                false,
                new Dictionary<string, object> {{DeadLetterExchangeArgument, deadLetterExchange}});
            channel.QueueBind(_rabbitSettings.ScheduleNotifyQueueName, _rabbitSettings.ExchangeName, _rabbitSettings.ScheduleNotifyRoutingKey);

            if (closeConnection)
            {
                channel.Close();
                channel.Dispose();
                connection.Close();
                connection.Dispose();
            }
            
            return (connection, channel);
        }
    }
}