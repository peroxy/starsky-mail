using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StarskyMail.Queue.Consumer.Services;
using StarskyMail.Queue.Extensions;
using StarskyMail.Queue.Models;
using StarskyMail.Queue.Settings;

namespace StarskyMail.Queue.Consumer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly SendGridService _sendGridService;
        private readonly RabbitMQSettings _rabbitSettings;

        public Worker(ILogger<Worker> logger, IOptions<RabbitMQSettings> rabbitSettings, QueueConfiguration queueConfiguration, SendGridService sendGridService)
        {
            _logger = logger;
            _queueConfiguration = queueConfiguration;
            _sendGridService = sendGridService;
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

        private async void OnInvitationsReceived(object sender, BasicDeliverEventArgs e)
        {
            var consumer = (EventingBasicConsumer) sender;

            string body = Encoding.UTF8.GetString(e.Body.ToArray());

            _logger.LogDebug($"New message in invitations queue:{Environment.NewLine}{body}");
            
            if (body.TryDeserializeJson(out InvitationsModel model))
            {
                await ValidateInvitationsAndSend(e.DeliveryTag, model, consumer);
            }
            else
            {
                consumer.Model.BasicReject(e.DeliveryTag, false);
                _logger.LogError($"Message rejected, could not deserialize JSON message into valid {nameof(InvitationsModel)} structure: {body}");
            }
            
        }

        private async Task ValidateInvitationsAndSend(ulong deliveryTag, InvitationsModel model, IBasicConsumer consumer)
        {
            (bool success, string reason) = model.Validate();
            if (success)
            {
                (string subject, string plainText, string html) = model.ToEmail();
                success = await _sendGridService.SendEmail(subject, model.EmployeeEmail, model.EmployeeName, plainText, html);
                if (success)
                {
                    _logger.LogDebug("Email processed successfully.");
                    consumer.Model.BasicAck(deliveryTag, false);
                    _logger.LogTrace("Message acknowledged.");
                }
                else
                {
                    _logger.LogDebug("Email sending failed.");
                    consumer.Model.BasicReject(deliveryTag, false);
                    _logger.LogError("Message rejected because SendGrid email sending failed.");
                }
            }
            else
            {
                consumer.Model.BasicReject(deliveryTag, false);
                _logger.LogError($"Message rejected due to: {reason}.");
            }
        }
    }
}