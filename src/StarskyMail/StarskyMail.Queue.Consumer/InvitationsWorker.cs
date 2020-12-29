using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace StarskyMail.Queue.Consumer
{
    public class InvitationsWorker : BackgroundService
    {
        private readonly ILogger<InvitationsWorker> _logger;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly RabbitMQSettings _rabbitSettings;

        public InvitationsWorker(ILogger<InvitationsWorker> logger, IOptions<RabbitMQSettings> rabbitSettings, QueueConfiguration queueConfiguration)
        {
            _logger = logger;
            _queueConfiguration = queueConfiguration;
            _rabbitSettings = rabbitSettings.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("The worker service has been started!");

            var connectionInfo = _queueConfiguration.CreateRabbitInfrastructure();
            using var connection = connectionInfo.connection;
            using var channel = connectionInfo.channel;

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += ConsumerOnReceived;
            channel.BasicConsume(_rabbitSettings.InvitationsQueueName, false, consumer);
            
            stoppingToken.WaitHandle.WaitOne(); // wait until cancellation token is canceled
            
            channel.Close();
            connection.Close();
            
            Console.WriteLine("The worker service has been stopped!");
            return Task.CompletedTask;
        }

        private static void ConsumerOnReceived(object sender, BasicDeliverEventArgs e)
        {
            var consumer = (EventingBasicConsumer) sender;
            Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}] New message: {Encoding.UTF8.GetString(e.Body.ToArray())}");
            consumer.Model.BasicReject(e.DeliveryTag, false);
        }
    }
}