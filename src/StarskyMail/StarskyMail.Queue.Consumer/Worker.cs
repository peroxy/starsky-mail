using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StarskyMail.Queue.Extensions;
using StarskyMail.Queue.Models;

namespace StarskyMail.Queue.Consumer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly RabbitMQSettings _rabbitSettings;

        public Worker(ILogger<Worker> logger, IOptions<RabbitMQSettings> rabbitSettings, QueueConfiguration queueConfiguration)
        {
            _logger = logger;
            _queueConfiguration = queueConfiguration;
            _rabbitSettings = rabbitSettings.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"The {nameof(Worker)} service has been started!");

            var connectionInfo = _queueConfiguration.CreateRabbitInfrastructure();
            using var connection = connectionInfo.connection;
            using var invitationsChannel = connectionInfo.channel;
            //using var emailVerificationsChannel = connection.CreateModel(); //use new channel for every new queue consumer..
            
            var invitationsConsumer = new EventingBasicConsumer(invitationsChannel);
            invitationsConsumer.Received += OnInvitationsReceived;
            invitationsChannel.BasicConsume(_rabbitSettings.InvitationsQueueName, false, invitationsConsumer);
            
            stoppingToken.WaitHandle.WaitOne(); // wait until cancellation token is canceled
            
            invitationsChannel.Close();
            connection.Close();

            Console.WriteLine($"The {nameof(Worker)} service has been stopped!");
            return Task.CompletedTask;
        }

        private void OnInvitationsReceived(object sender, BasicDeliverEventArgs e)
        {
            var consumer = (EventingBasicConsumer) sender;

            string body = Encoding.UTF8.GetString(e.Body.ToArray());

            _logger.LogDebug($"New message in invitations queue:{Environment.NewLine}{body}");
            
            if (body.TryDeserializeJson(out InvitationsModel model))
            {
                (bool success, string reason) = model.Validate();
                if (success)
                {
                    
                    _logger.LogTrace("Email has been sent.");
                    consumer.Model.BasicAck(e.DeliveryTag, false);
                    _logger.LogTrace("Message acknowledged.");
                }
                else
                {
                    consumer.Model.BasicReject(e.DeliveryTag, false);
                    _logger.LogError($"Message rejected due to: {reason}.");
                }
            }
            else
            {
                consumer.Model.BasicReject(e.DeliveryTag, false);
                _logger.LogError($"Message rejected, could not deserialize JSON message into valid {nameof(InvitationsModel)} structure: {body}");
            }
            
        }
    }
}