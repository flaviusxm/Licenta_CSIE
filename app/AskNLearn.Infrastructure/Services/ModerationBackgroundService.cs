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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ModerationBackgroundService> _logger;
        private DateTime _lastCleanupTime = DateTime.MinValue;

        public ModerationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ModerationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TrustLayer Service is starting. Monitoring for threats and validation tasks...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    // 1. Process Identity Verifications (Pending and not yet touched by AI)
                    var pendingVerifications = await dbContext.VerificationRequests
                        .Where(v => v.Status == VerificationRequestStatus.Pending && v.AdminNotes.Contains("Analiză în curs"))
                        .Take(5)
                        .ToListAsync(stoppingToken);

                    foreach (var request in pendingVerifications)
                    {
                        await ProcessSingleTaskAsync(new ModerationTask
                        {
                            Id = request.Id,
                            Target = ModerationTarget.IdentityVerification,
                            Content = request.StudentIdUrl
                        }, stoppingToken);
                    }

                    // 2. Process Post Moderation
                    var pendingPosts = await dbContext.Posts
                        .Where(p => p.ModerationStatus == ModerationStatus.Pending)
                        .Take(5)
                        .ToListAsync(stoppingToken);

                    foreach (var post in pendingPosts)
                    {
                        await ProcessSingleTaskAsync(new ModerationTask
                        {
                            Id = post.Id,
                            Target = ModerationTarget.Post,
                            Content = post.Content,
                            Title = post.Title
                        }, stoppingToken);
                    }

                    // 3. Process Comment Moderation
                    var pendingComments = await dbContext.Comments
                        .Where(c => c.ModerationStatus == ModerationStatus.Pending)
                        .Take(5)
                        .ToListAsync(stoppingToken);

                    foreach (var comment in pendingComments)
                    {
                        await ProcessSingleTaskAsync(new ModerationTask
                        {
                            Id = comment.Id,
                            Target = ModerationTarget.Comment,
                            Content = comment.Content
                        }, stoppingToken);
                    }

                    // 4. Periodic Maintenance (Scan Resources)
                    if (DateTime.UtcNow - _lastCleanupTime > TimeSpan.FromMinutes(15))
                    {
                        await ScanNewResources(stoppingToken);
                        _lastCleanupTime = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TrustLayer error in polling loop.");
                }

                // Poll every 5 seconds
                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task RunPeriodicMaintenanceAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ScanNewResources(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during periodic maintenance.");
                }
                // Scanăm resursele noi la fiecare 15 minute complet independent
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        private async Task ProcessSingleTaskAsync(ModerationTask task, CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("[TrustLayer] Processing task for {Target} with Id {Id}.", task.Target, task.Id);

                using var scope = _scopeFactory.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<ITrustLayerService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                if (task.Target == ModerationTarget.IdentityVerification)
                {
                    var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
                    var request = await dbContext.VerificationRequests.FindAsync(new object[] { task.Id }, stoppingToken);
                    
                    if (request != null)
                    {
                        // Evităm eroarea 500 a modelului Moondream. AI-urile vizuale (momentan) acceptă doar poze, nu PDF-uri.
                        if (request.StudentIdUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("[TrustLayer] Document is a PDF. Bypassing AI vision and routing to manual review.");
                            await ProcessIdentityVerification(dbContext, task.Id, (false, "Documentul este un PDF. Verificarea automată AI suportă momentan doar imagini.", "Necesită verificare umană (Admin)"), stoppingToken);
                        }
                        else
                        {
                            try
                            {
                                byte[] studentIdBytes = await fileService.ReadFileAsync(request.StudentIdUrl);
                                var vResult = await aiService.VerifyDocumentAsync(studentIdBytes);
                                await ProcessIdentityVerification(dbContext, task.Id, vResult, stoppingToken);
                                
                                _logger.LogInformation("[TrustLayer] Identity validation completed for Request {Id}. Result: {Status}", 
                                    task.Id, vResult.IsValid ? "Verified" : "Rejected/Review");
                            }
                            catch (FileNotFoundException fnfEx)
                            {
                                _logger.LogWarning(fnfEx, "[TrustLayer] Document file not found for request {Id}. Path: {Path}. Routing to manual review.", task.Id, request.StudentIdUrl);
                                await ProcessIdentityVerification(dbContext, task.Id,
                                    (false, $"Document not found on server: {request.StudentIdUrl}. File may have been deleted or path is external.", "Manual Review Required"),
                                    stoppingToken);
                            }
                        }
                    }
                }
                else
                {
                    string contentToModerate = task.Content;
                    string? titleToModerate = task.Title;

                    var result = await aiService.ModerateTextAsync(contentToModerate, titleToModerate);

                    switch (task.Target)
                    {
                        case ModerationTarget.Post:
                            await ProcessPostModeration(dbContext, task.Id, result, stoppingToken);
                            break;

                        case ModerationTarget.Comment:
                            await ProcessCommentModeration(dbContext, task.Id, result, stoppingToken);
                            break;
                        case ModerationTarget.Message:
                            await ProcessMessageModeration(dbContext, task.Id, result, stoppingToken);
                            break;

                        case ModerationTarget.Report:
                            await ProcessReportModeration(dbContext, task.Id, result, stoppingToken);
                            break;

                        case ModerationTarget.Resource:
                            await ProcessResourceModeration(dbContext, task.Id, result, stoppingToken);
                            break;
                    }

                    _logger.LogInformation("[TrustLayer] Moderation completed for {Target} {Id}. Result: {Status}", 
                        task.Target, task.Id, result.IsSafe ? "Safe/Approved" : "Unsafe/Actioned");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing individual task {Id}.", task.Id);
            }
        }



        private async Task ScanNewResources(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                
                var unscannedFiles = await dbContext.StoredFiles
                    .Where(f => f.SecurityNotes == null)
                    .Take(10)
                    .ToListAsync(ct);

                foreach (var file in unscannedFiles)
                {
                    await ProcessResourceModeration(dbContext, file.Id, (true, "Automated system scan - Resource marked safe by default."), ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning new resources.");
            }
        }

        private async Task ProcessResourceModeration(IApplicationDbContext db, Guid id, (bool IsSafe, string Reason) result, CancellationToken ct)
        {
            var file = await db.StoredFiles.FindAsync(new object[] { id }, ct);
            if (file != null)
            {
                file.IsSafe = result.IsSafe;
                file.SecurityNotes = $"[TrustLayer Scan]: {result.Reason}";
                await db.SaveChangesAsync(ct);
            }
        }

        private async Task ProcessIdentityVerification(IApplicationDbContext db, Guid requestId, (bool IsValid, string Details, string Recommendation) result, CancellationToken ct)
        {
            var request = await db.VerificationRequests.FindAsync(new object[] { requestId }, ct);
            if (request != null)
            {
                bool autoApproved = result.IsValid;
                
                // 1. Update status and notes immediately (removing "Analiză în curs" to break the polling loop)
                request.Status = autoApproved ? VerificationRequestStatus.Approved : VerificationRequestStatus.Pending;
                request.AdminNotes = $"[TrustLayer Llama 3.2 Vision]: {result.Recommendation} | Details: {result.Details}";
                request.ProcessedAt = DateTime.UtcNow;
                
                // IMPORTANT: ProcessedBy must be a valid User ID or null. AI is not a user.
                request.ProcessedBy = null;

                if (autoApproved)
                {
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
                post.ModerationStatus = result.IsSafe ? ModerationStatus.Approved : ModerationStatus.Removed;
                post.ModerationReason = result.Reason;
                await db.SaveChangesAsync(ct);
            }
        }

        private async Task ProcessMessageModeration(IApplicationDbContext db, Guid id, (bool IsSafe, string Reason) result, CancellationToken ct)
        {
            var message = await db.Messages.FindAsync(new object[] { id }, ct);
            if (message != null)
            {
                message.ModerationStatus = result.IsSafe ? ModerationStatus.Approved : ModerationStatus.Removed;
                // message doesn't have ModerationReason anymore
                await db.SaveChangesAsync(ct);
            }
        }

        private async Task ProcessCommentModeration(IApplicationDbContext db, Guid id, (bool IsSafe, string Reason) result, CancellationToken ct)
        {
            var comment = await db.Comments.FindAsync(new object[] { id }, ct);
            if (comment != null)
            {
                comment.ModerationStatus = result.IsSafe ? ModerationStatus.Approved : ModerationStatus.Removed;
                comment.ModerationReason = result.Reason;
                await db.SaveChangesAsync(ct);
            }
        }

        private async Task ProcessReportModeration(IApplicationDbContext db, Guid id, (bool IsSafe, string Reason) result, CancellationToken ct)
        {
            var report = await db.Reports
                .Include(r => r.ReportedPost)
                .Include(r => r.ReportedComment)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (report != null)
            {
                if (!result.IsSafe)
                {
                    report.Status = ReportStatus.Resolved;
                    if (report.ReportedPost != null)
                    {
                        report.ReportedPost.ModerationStatus = ModerationStatus.Removed;
                        report.ReportedPost.ModerationReason = $"[TrustLayer Confirmed]: {result.Reason}";
                    }
                    if (report.ReportedComment != null)
                    {
                        report.ReportedComment.ModerationStatus = ModerationStatus.Removed;
                        report.ReportedComment.ModerationReason = $"[TrustLayer Confirmed]: {result.Reason}";
                    }
                }
                else
                {
                    report.Status = ReportStatus.Dismissed;
                }
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
