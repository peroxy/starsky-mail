using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace StarskyMail.Queue.Consumer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions<RabbitMQSettings>()
                        .Bind(hostContext.Configuration.GetSection(RabbitMQSettings.Section))
                        .ValidateDataAnnotations();
            
                    services.AddSingleton<QueueConfiguration>();
                    services.AddHostedService<Worker>();
                });
    }
}