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
        private DateTime _lastCleanupTime = DateTime.MinValue;

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
                    // 1. Process Queue Tasks (Non-blocking check)
                    var task = await _queue.DequeueAsync(stoppingToken);
                    if (task != null)
                    {
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
                            string contentToModerate = task.Content;
                            string? titleToModerate = task.Title;

                            var result = await guardianClient.ModerateTextAsync(contentToModerate, titleToModerate);

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

                    // 2. Periodic Maintenance (Maintenance Cycle)
                    if (DateTime.UtcNow - _lastCleanupTime > TimeSpan.FromHours(1))
                    {
                        await PerformMaintenanceTasks(stoppingToken);
                        _lastCleanupTime = DateTime.UtcNow;
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Guardian Shield error processing task.");
                    await Task.Delay(1000, stoppingToken); // Prevent infinite fast loop on error
                }
            }
        }

        private async Task PerformMaintenanceTasks(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                _logger.LogInformation("[Shield] Running maintenance tasks (Cleanup)...");

                var cutoff = DateTime.UtcNow.AddDays(-1);
                var unconfirmedUsers = await dbContext.Users
                    .Where(u => !u.EmailConfirmed && u.CreatedAt < cutoff)
                    .ToListAsync(ct);

                if (unconfirmedUsers.Any())
                {
                    var userIds = unconfirmedUsers.Select(u => u.Id).ToList();

                    // Cascade deletes
                    var friendships = await dbContext.Friendships
                        .Where(f => userIds.Contains(f.RequesterId) || userIds.Contains(f.AddresseeId))
                        .ToListAsync(ct);
                    if (friendships.Any()) dbContext.Friendships.RemoveRange(friendships);

                    var participants = await dbContext.DirectConversationParticipants
                        .Where(p => userIds.Contains(p.UserId))
                        .ToListAsync(ct);
                    if (participants.Any()) dbContext.DirectConversationParticipants.RemoveRange(participants);

                    var communityMembers = await dbContext.CommunityMemberships
                        .Where(m => userIds.Contains(m.UserId))
                        .ToListAsync(ct);
                    if (communityMembers.Any()) dbContext.CommunityMemberships.RemoveRange(communityMembers);

                    var groupMembers = await dbContext.GroupMemberships
                        .Where(m => userIds.Contains(m.UserId))
                        .ToListAsync(ct);
                    if (groupMembers.Any()) dbContext.GroupMemberships.RemoveRange(groupMembers);

                    var notifications = await dbContext.Notifications
                        .Where(n => userIds.Contains(n.UserId))
                        .ToListAsync(ct);
                    if (notifications.Any()) dbContext.Notifications.RemoveRange(notifications);

                    var verRequests = await dbContext.VerificationRequests
                        .Where(v => userIds.Contains(v.UserId))
                        .ToListAsync(ct);
                    if (verRequests.Any()) dbContext.VerificationRequests.RemoveRange(verRequests);

                    var posts = await dbContext.Posts
                        .Where(p => userIds.Contains(p.AuthorId))
                        .ToListAsync(ct);
                    if (posts.Any()) dbContext.Posts.RemoveRange(posts);

                    var messages = await dbContext.Messages
                        .Where(m => userIds.Contains(m.AuthorId))
                        .ToListAsync(ct);
                    if (messages.Any()) dbContext.Messages.RemoveRange(messages);

                    dbContext.Users.RemoveRange(unconfirmedUsers);
                    await dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("[Shield] Cleaned up {Count} unconfirmed accounts.", unconfirmedUsers.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Guardian Shield maintenance error.");
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
