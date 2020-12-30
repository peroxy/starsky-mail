using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StarskyMail.Queue.Consumer.Services;
using StarskyMail.Queue.Settings;

namespace StarskyMail.Queue.Consumer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        builder.AddUserSecrets<Program>();
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions<RabbitMQSettings>()
                        .Bind(hostContext.Configuration.GetSection(RabbitMQSettings.Section))
                        .ValidateDataAnnotations();

                    services.AddOptions<SendGridSettings>()
                        .Bind(hostContext.Configuration.GetSection(SendGridSettings.Section))
                        .ValidateDataAnnotations()
                        .Validate(ValidateSendGridSettings, "API key must be valid if SendGrid is enabled!");

                    services.AddSingleton<QueueConfiguration>();

                    services.AddTransient<SendGridService>();

                    services.AddHostedService<Worker>();
                });
        }

        /// <summary>
        /// API key must be specified if send grid sending is enabled. Otherwise just ignore the api key requirement.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static bool ValidateSendGridSettings(SendGridSettings config)
        {
            return !config.Enabled || !string.IsNullOrWhiteSpace(config.ApiKey);
        }
    }
}