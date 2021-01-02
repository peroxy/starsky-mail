using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using StarskyMail.Queue.Settings;

namespace StarskyMail.Queue.Consumer.Services
{
    public class SendGridService
    {
        private readonly ILogger<SendGridService> _logger;
        private readonly SendGridSettings _settings;

        public SendGridService(ILogger<SendGridService> logger, IOptions<SendGridSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
            _logger.LogInformation($"Configuration - SendGrid sending enabled: {_settings.Enabled}");
        }

        public async Task<bool> SendEmail(string fromAddress, string toAddress, string toName, string templateId, object templateData, int unsubscribeGroupId)
        {
            if (!_settings.Enabled)
            {
                _logger.LogWarning("SendGrid sending is disabled in appsettings. Will not send an email.");
                return true;
            }

            var client = new SendGridClient(_settings.ApiKey);
            var from = new EmailAddress(fromAddress, "Starsky Scheduling");
            var to = new EmailAddress(toAddress, toName);
            
            try
            {
                var email = MailHelper.CreateSingleTemplateEmail(from, to, templateId, templateData);
                email.SetAsm(unsubscribeGroupId, new List<int>(1) {unsubscribeGroupId});
                var response = await client.SendEmailAsync(email);
                _logger.LogDebug($"SendGrid API response code: {response.StatusCode}, is success status code: {response.IsSuccessStatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return false;
            }
        }
    }

}