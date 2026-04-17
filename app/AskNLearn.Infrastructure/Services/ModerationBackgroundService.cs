using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
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
            _logger.LogInformation("Guardian Shield Service is starting. Monitoring for threats and validation tasks...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var task = await _queue.DequeueAsync(stoppingToken);
                    _logger.LogInformation("[Shield] Processing task for {Target} with Id {Id}.", task.Target, task.Id);

                    using var scope = _scopeFactory.CreateScope();
                    var guardianClient = scope.ServiceProvider.GetRequiredService<IGuardianClient>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    if (task.Target == ModerationTarget.IdentityVerification)
                    {
                        var vResult = await guardianClient.VerifyDocumentAsync(null, task.Content);
                        await ProcessIdentityVerification(dbContext, task.Id, vResult, stoppingToken);
                        _logger.LogInformation("[Shield] Identity validation completed for Request {Id}. Result: {Status}", 
                            task.Id, vResult.IsValid ? "Verified" : "Rejected/Review");
                    }
                    else
                    {
                        // 1. Determine Content to Moderate
                        string contentToModerate = task.Content;
                        string? titleToModerate = task.Title;

                        // 2. Run AI Moderation
                        var result = await guardianClient.ModerateTextAsync(contentToModerate, titleToModerate);

                        // 3. Autonomous Action based on Target
                        switch (task.Target)
                        {
                            case ModerationTarget.Post:
                                await ProcessPostModeration(dbContext, task.Id, result, stoppingToken);
                                break;

                            case ModerationTarget.Comment:
                            case ModerationTarget.Message:
                                await ProcessMessageModeration(dbContext, task.Id, result, stoppingToken);
                                break;

                            case ModerationTarget.Report:
                                await ProcessReportModeration(dbContext, task.Id, result, stoppingToken);
                                break;
                        }

                        _logger.LogInformation("[Shield] Moderation completed for {Target} {Id}. Result: {Status}", 
                            task.Target, task.Id, result.IsSafe ? "Safe/Approved" : "Unsafe/Actioned");
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Guardian Shield error processing task.");
                }
            }
        }

        private async Task ProcessIdentityVerification(IApplicationDbContext db, Guid requestId, (bool IsValid, string Details, string Recommendation) result, CancellationToken ct)
        {
            var request = await db.VerificationRequests.FindAsync(new object[] { requestId }, ct);
            if (request != null)
            {
                bool autoApproved = result.IsValid && result.Recommendation.Contains("Approved", StringComparison.OrdinalIgnoreCase);
                
                request.Status = autoApproved ? Status.Approved : Status.Pending;
                request.AdminNotes = $"[Guardian Shield AI]: {result.Recommendation} | Details: {result.Details}";
                
                if (autoApproved)
                {
                    request.ProcessedAt = DateTime.UtcNow;
                    request.ProcessedBy = "SYSTEM_AI_SHIELD";
                    
                    var user = await db.Users.FindAsync(new object[] { request.UserId }, ct);
                    if (user != null)
                    {
                        user.IsVerified = true;
                        user.VerificationStatus = UserVerificationStatus.IdentityVerified;
                    }
                }
                
                await db.SaveChangesAsync(ct);
            }
        }

        private async Task ProcessPostModeration(IApplicationDbContext db, Guid id, (bool IsSafe, string Reason) result, CancellationToken ct)
        {
            var post = await db.Posts.FindAsync(new object[] { id }, ct);
            if (post != null)
            {
                post.ModerationStatus = result.IsSafe ? ModerationStatus.Approved : ModerationStatus.Flagged;
                post.ModerationReason = result.Reason;
                await db.SaveChangesAsync(ct);
            }
        }

        private async Task ProcessMessageModeration(IApplicationDbContext db, Guid id, (bool IsSafe, string Reason) result, CancellationToken ct)
        {
            var message = await db.Messages.FindAsync(new object[] { id }, ct);
            if (message != null)
            {
                message.ModerationStatus = result.IsSafe ? ModerationStatus.Approved : ModerationStatus.Flagged;
                message.ModerationReason = result.Reason;
                await db.SaveChangesAsync(ct);
            }
        }

        private async Task ProcessReportModeration(IApplicationDbContext db, Guid id, (bool IsSafe, string Reason) result, CancellationToken ct)
        {
            var report = await db.Reports
                .Include(r => r.ReportedPost)
                .Include(r => r.ReportedMessage)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (report != null)
            {
                // If AI confirms it's unsafe, remove the content and resolve report
                if (!result.IsSafe)
                {
                    report.Status = ReportStatus.Resolved;
                    
                    if (report.ReportedPost != null)
                    {
                        report.ReportedPost.ModerationStatus = ModerationStatus.Removed;
                        report.ReportedPost.ModerationReason = $"[Guardian Shield - AI Confirmed Report]: {result.Reason}";
                    }
                    if (report.ReportedMessage != null)
                    {
                        report.ReportedMessage.ModerationStatus = ModerationStatus.Removed;
                        report.ReportedMessage.ModerationReason = $"[Guardian Shield - AI Confirmed Report]: {result.Reason}";
                    }
                }
                else
                {
                    // If AI says it's safe despite report, mark report but keep content visible
                    report.Status = ReportStatus.Dismissed;
                }
                
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
