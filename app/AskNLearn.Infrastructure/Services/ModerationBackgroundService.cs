using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Infrastructure.Services
{
    public class ModerationBackgroundService : BackgroundService
    {
        private readonly IModerationQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ModerationBackgroundService> _logger;

        public ModerationBackgroundService(
            IModerationQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<ModerationBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Moderation Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var task = await _queue.DequeueAsync(stoppingToken);
                    _logger.LogInformation("Processing moderation task for {Target} with Id {Id}.", task.Target, task.Id);

                    using var scope = _scopeFactory.CreateScope();
                    var guardianClient = scope.ServiceProvider.GetRequiredService<IGuardianClient>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    var result = await guardianClient.ModerateTextAsync(task.Content, task.Title);

                    if (task.Target == ModerationTarget.Post)
                    {
                        var post = await dbContext.Posts.FindAsync(new object[] { task.Id }, stoppingToken);
                        if (post != null)
                        {
                            post.ModerationStatus = result.IsSafe ? ModerationStatus.Approved : ModerationStatus.Flagged;
                            post.ModerationReason = result.Reason;
                            await dbContext.SaveChangesAsync(stoppingToken);
                        }
                    }
                    else if (task.Target == ModerationTarget.Comment)
                    {
                        var comment = await dbContext.Messages.FindAsync(new object[] { task.Id }, stoppingToken);
                        if (comment != null)
                        {
                            comment.ModerationStatus = result.IsSafe ? ModerationStatus.Approved : ModerationStatus.Flagged;
                            comment.ModerationReason = result.Reason;
                            await dbContext.SaveChangesAsync(stoppingToken);
                        }
                    }

                    _logger.LogInformation("Moderation completed for {Target} {Id}. Result: {Status}", 
                        task.Target, task.Id, result.IsSafe ? "Approved" : "Flagged");
                }
                catch (OperationCanceledException)
                {
                    // Exit gracefully
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing moderation task.");
                }
            }
        }
    }
}
