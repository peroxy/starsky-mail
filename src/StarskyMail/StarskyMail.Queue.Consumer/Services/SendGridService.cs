using System;
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
        }

        public async Task<bool> SendEmail(string subject, string toAddress, string toName, string plainTextContent, string htmlContent)
        {
            if (!_settings.Enabled)
            {
                _logger.LogDebug("SendGrid sending is disabled in appsettings. Will not send an email.");
                return true;
            }

            var client = new SendGridClient(_settings.ApiKey);
            var from = new EmailAddress(_settings.FromAddress, "Starsky Scheduling");
            var to = new EmailAddress(toAddress, toName);
            
            var email = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            try
            {
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