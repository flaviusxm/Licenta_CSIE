using AskNLearn.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AskNLearn.Infrastructure.Services
{
    public class EmailConfirmationCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailConfirmationCleanupService> _logger;

        public EmailConfirmationCleanupService(IServiceScopeFactory scopeFactory, ILogger<EmailConfirmationCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    var cutoff = DateTime.UtcNow.AddDays(-1);
                    var unconfirmedUsers = dbContext.Users
                        .Where(u => !u.EmailConfirmed && u.CreatedAt < cutoff)
                        .ToList();

                    if (unconfirmedUsers.Any())
                    {
                        dbContext.Users.RemoveRange(unconfirmedUsers);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Deleted {Count} unconfirmed user accounts older than 24h.", unconfirmedUsers.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during email confirmation cleanup.");
                }

                // Rulează o dată pe oră
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}