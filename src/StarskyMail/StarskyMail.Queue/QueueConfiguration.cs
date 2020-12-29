using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

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

        /// <summary>
        /// Code-first approach to RabbitMQ queues and exchanges - will create everything from scratch if it does not exist, otherwise it just checks everything is OK.
        /// </summary>
        public (IConnection connection, IModel channel) CreateRabbitInfrastructure(bool closeConnection = false)
        {
            var connection = GetConnection();
            var channel = connection.CreateModel();

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