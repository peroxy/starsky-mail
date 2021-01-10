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
        private readonly ConnectionFactory _factory;
        private readonly RabbitMQSettings _rabbitSettings;
        
        private const string DeadLetterExchangeArgument = "x-dead-letter-exchange";

        public QueueConfiguration(IOptions<RabbitMQSettings> configuration)
        {
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

        public void TryConnect(ILogger logger, int retryCount)
        {
            for (int i = 0; i < retryCount; i++)
            {
                (bool reachable, var brokerUnreachableException) = IsRabbitReachable();
                if (reachable)
                {
                    logger.LogInformation($"RabbitMQ broker is reachable! Success!");
                    break;
                }
                else
                {
                    if (i == retryCount - 1)
                    {
                        logger.LogCritical($"RabbitMQ broker is unreachable! Can't establish a connection, will stop retrying.");
                        throw brokerUnreachableException;
                    }
                    else
                    {
                        logger.LogWarning($"[#{i}] RabbitMQ broker is unreachable - retrying in 5 seconds..");
                        Thread.Sleep(TimeSpan.FromSeconds(5));                        
                    }
                }
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
            var deadLetterQueue = $"dead.letter.{_rabbitSettings.InvitationsQueueName}";
            
            channel.ExchangeDeclare(deadLetterExchange, ExchangeType.Fanout, true);
            channel.QueueDeclare(deadLetterQueue, true, false, false);
            channel.QueueBind(deadLetterQueue, deadLetterExchange, "dead.letter");
            
            channel.ExchangeDeclare(_rabbitSettings.ExchangeName, ExchangeType.Direct, true);
            
            channel.QueueDeclare(_rabbitSettings.InvitationsQueueName,
                true,
                false,
                false,
                new Dictionary<string, object> {{DeadLetterExchangeArgument, deadLetterExchange}});
            channel.QueueBind(_rabbitSettings.InvitationsQueueName, _rabbitSettings.ExchangeName, _rabbitSettings.InvitationsRoutingKey);

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