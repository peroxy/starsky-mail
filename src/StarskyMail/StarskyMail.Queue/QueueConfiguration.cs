using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace StarskyMail.Queue
{
    public class QueueConfiguration
    {
        private readonly ConnectionFactory _factory;
        
        public QueueConfiguration(IOptions<RabbitMQSettings> configuration)
        {
            var config = configuration.Value;
            
            _factory = new ConnectionFactory
            {
                UserName = config.Username,
                Password = config.Password,
                HostName = config.Hostname,
                Port = config.Port
            };
        }
        
        public IConnection GetConnection()
        {
            return _factory.CreateConnection();
        }
    }
}