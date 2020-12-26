using System;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace StarskyMail.Queue
{
    public class QueueConfiguration
    {
        private readonly ConnectionFactory _factory;
        private const string UserEnvironmentVariable = "RABBITMQ_DEFAULT_USER";
        private const string PasswordEnvironmentVariable = "RABBITMQ_DEFAULT_PASS";
        
        public QueueConfiguration(IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration[UserEnvironmentVariable]))
            {
                throw new InvalidOperationException($"{UserEnvironmentVariable} environment variable not set!");
            }
            
            if (string.IsNullOrWhiteSpace(configuration[PasswordEnvironmentVariable]))
            {
                throw new InvalidOperationException($"{PasswordEnvironmentVariable} environment variable not set!");
            }
            
            _factory = new ConnectionFactory
            {
                UserName = configuration[UserEnvironmentVariable],
                Password = configuration[PasswordEnvironmentVariable],
            };
            
        }
        
        public IConnection GetConnection()
        {
            return _factory.CreateConnection();
        }
    }
}