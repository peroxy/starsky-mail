using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace StarskyMail.Queue.Api.Services
{
    /// <summary>
    /// Service that executes code right after Startup has done constructing and configuring everything.
    /// </summary>
    public class AfterStartupService : IHostedService
    {
        private readonly QueueConfiguration _configuration;

        public AfterStartupService(QueueConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _configuration.CreateRabbitInfrastructure(true);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}