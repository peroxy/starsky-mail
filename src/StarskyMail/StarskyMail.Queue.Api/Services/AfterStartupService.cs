using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StarskyMail.Queue.Api.Services
{
    /// <summary>
    /// Service that executes code right after Startup has done constructing and configuring everything.
    /// </summary>
    public class AfterStartupService : IHostedService
    {
        private readonly ILogger<AfterStartupService> _logger;
        private readonly QueueConfiguration _configuration;

        public AfterStartupService(ILogger<AfterStartupService> logger, QueueConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _configuration.TryConnect(_logger, 5);
            _configuration.CreateRabbitInfrastructure(true);

            _logger.LogInformation("Created RabbitMQ infrastructure sucessfully.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}