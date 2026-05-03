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

            // Rulăm mentenanța (ex: scanarea fișierelor) într-un task separat pentru a nu bloca coada principală
            _ = Task.Run(() => RunPeriodicMaintenanceAsync(stoppingToken), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 1. Process Queue Tasks (Așteaptă un task nou)
                    var task = await _queue.DequeueAsync(stoppingToken);
                    if (task != null)
                    {
                        // Procesăm taskul curent într-un background task ca să nu blocăm coada
                        _ = ProcessSingleTaskAsync(task, stoppingToken);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Guardian Shield error in queue listener.");
                    await Task.Delay(1000, stoppingToken); // Prevent infinite fast loop on error
                }
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
                _logger.LogInformation("[Shield] Processing task for {Target} with Id {Id}.", task.Target, task.Id);

                using var scope = _scopeFactory.CreateScope();
                var guardianClient = scope.ServiceProvider.GetRequiredService<IGuardianClient>();
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
                            _logger.LogInformation("[Shield] Document is a PDF. Bypassing AI vision and routing to manual review.");
                            await ProcessIdentityVerification(dbContext, task.Id, (false, "Documentul este un PDF. Verificarea automată AI suportă momentan doar imagini.", "Necesită verificare umană (Admin)"), stoppingToken);
                        }
                        else
                        {
                            byte[] studentIdBytes = await fileService.ReadFileAsync(request.StudentIdUrl);
                            var vResult = await guardianClient.VerifyDocumentAsync(studentIdBytes);
                            await ProcessIdentityVerification(dbContext, task.Id, vResult, stoppingToken);
                            
                            _logger.LogInformation("[Shield] Identity validation completed for Request {Id}. Result: {Status}", 
                                task.Id, vResult.IsValid ? "Verified" : "Rejected/Review");
                        }
                    }
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

                        case ModerationTarget.Resource:
                            await ProcessResourceModeration(dbContext, task.Id, result, stoppingToken);
                            break;
                    }

                    _logger.LogInformation("[Shield] Moderation completed for {Target} {Id}. Result: {Status}", 
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
                    _queue.Enqueue(new ModerationTask 
                    { 
                        Id = file.Id, 
                        Target = ModerationTarget.Resource, 
                        Content = file.FileName,
                        Title = file.ModuleContext
                    });
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
                file.SecurityNotes = $"[Shield AI Scan]: {result.Reason}";
                await db.SaveChangesAsync(ct);
            }
        }

        private async Task ProcessIdentityVerification(IApplicationDbContext db, Guid requestId, (bool IsValid, string Details, string Recommendation) result, CancellationToken ct)
        {
            var request = await db.VerificationRequests.FindAsync(new object[] { requestId }, ct);
            if (request != null)
            {
                bool autoApproved = result.IsValid;
                
                request.Status = autoApproved ? Status.Approved : Status.Pending;
                request.AdminNotes = $"[Guardian Shield Moondream AI]: {result.Recommendation} | Details: {result.Details}";
                
                if (autoApproved)
                {
                    request.ProcessedAt = DateTime.UtcNow;
                    request.ProcessedBy = "SYSTEM_AI_GUARDIAN";
                    
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
                if (!result.IsSafe)
                {
                    report.Status = ReportStatus.Resolved;
                    if (report.ReportedPost != null)
                    {
                        report.ReportedPost.ModerationStatus = ModerationStatus.Removed;
                        report.ReportedPost.ModerationReason = $"[Shield AI Confirmed]: {result.Reason}";
                    }
                    if (report.ReportedMessage != null)
                    {
                        report.ReportedMessage.ModerationStatus = ModerationStatus.Removed;
                        report.ReportedMessage.ModerationReason = $"[Shield AI Confirmed]: {result.Reason}";
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
