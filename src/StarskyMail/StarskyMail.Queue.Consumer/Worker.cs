using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using StarskyMail.Queue.Consumer.Services;
using StarskyMail.Queue.Extensions;
using StarskyMail.Queue.Models;
using StarskyMail.Queue.Settings;

namespace StarskyMail.Queue.Consumer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SendGridSettings _sendGridSettings;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly SendGridService _sendGridService;
        private readonly RabbitMQSettings _rabbitSettings;

        public Worker(ILogger<Worker> logger, IOptions<RabbitMQSettings> rabbitSettings, IOptions<SendGridSettings> sendGridSettings,
            QueueConfiguration queueConfiguration, SendGridService sendGridService)
        {
            _logger = logger;
            _sendGridSettings = sendGridSettings.Value;
            _queueConfiguration = queueConfiguration;
            _sendGridService = sendGridService;
            _rabbitSettings = rabbitSettings.Value;

            _logger.LogInformation($"DOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"The {nameof(Worker)} service has been started!");

            _queueConfiguration.TryConnect(5);

            var connectionInfo = _queueConfiguration.CreateRabbitInfrastructure();
            using var connection = connectionInfo.connection;
            using var invitationsChannel = connectionInfo.channel;
            using var scheduleNotifyChannel = connection.CreateModel(); //use new channel for every new queue consumer..

            var invitationsConsumer = new EventingBasicConsumer(invitationsChannel);
            invitationsConsumer.Received += OnInvitationsReceived;
            invitationsChannel.BasicConsume(_rabbitSettings.InvitationsQueueName, false, invitationsConsumer);
            
            var scheduleNotifyConsumer = new EventingBasicConsumer(scheduleNotifyChannel);
            scheduleNotifyConsumer.Received += OnScheduleNotifyReceived;
            scheduleNotifyChannel.BasicConsume(_rabbitSettings.ScheduleNotifyQueueName, false, scheduleNotifyConsumer);

            stoppingToken.WaitHandle.WaitOne(); // wait until cancellation token is canceled

            invitationsChannel.Close();
            scheduleNotifyChannel.Close();
            connection.Close();

            Console.WriteLine($"The {nameof(Worker)} service has been stopped!");
            return Task.CompletedTask;
        }
        
        private async void OnScheduleNotifyReceived(object sender, BasicDeliverEventArgs e)
        {
            var consumer = (EventingBasicConsumer) sender;
            string body = Encoding.UTF8.GetString(e.Body.ToArray());
            _logger.LogInformation($"New message in schedule notify queue:{Environment.NewLine}{body}");

            if (body.TryDeserializeJson(out ScheduleNotifyModel model))
            {
                await ValidateAndSend(e.DeliveryTag, model, consumer, model.EmployeeEmail, model.EmployeeName, _sendGridSettings.ScheduleNotificationTemplateId);
            }
            else
            {
                consumer.Model.BasicReject(e.DeliveryTag, false);
                _logger.LogError($"Message rejected, could not deserialize JSON message into valid {nameof(InvitationsMailModel)} structure: {body}");
            }
        }

        private async void OnInvitationsReceived(object sender, BasicDeliverEventArgs e)
        {
            var consumer = (EventingBasicConsumer) sender;
            string body = Encoding.UTF8.GetString(e.Body.ToArray());
            _logger.LogInformation($"New message in invitations queue:{Environment.NewLine}{body}");

            if (body.TryDeserializeJson(out InvitationsMailModel model))
            {
                await ValidateAndSend(e.DeliveryTag, model, consumer, model.EmployeeEmail, model.EmployeeName, _sendGridSettings.InvitationsTemplateId);
            }
            else
            {
                consumer.Model.BasicReject(e.DeliveryTag, false);
                _logger.LogError($"Message rejected, could not deserialize JSON message into valid {nameof(InvitationsMailModel)} structure: {body}");
            }
        }

        private async Task ValidateAndSend(ulong deliveryTag, IMailModel model, IBasicConsumer consumer, string toAddress, string toName, string templateId)
        {
            (bool success, string reason) = model.Validate();
            if (success)
            {
                var templateData = model.ToDynamicTemplateData();

                success = await _sendGridService.SendEmail(_sendGridSettings.FromAddress, toAddress, toName, templateId, templateData, _sendGridSettings.UnsubscribeGroupId);
                
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