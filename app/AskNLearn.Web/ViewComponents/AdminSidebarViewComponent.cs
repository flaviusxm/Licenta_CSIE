using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AskNLearn.Web.ViewComponents
{
    public class AdminSidebarViewComponent : ViewComponent
    {
        private readonly IApplicationDbContext _context;

        public AdminSidebarViewComponent(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var pendingVerifications = await _context.VerificationRequests.CountAsync(v => v.Status == Status.Pending);
            
            // Only count items flagged by AI or needing review
            var flaggedPosts = await _context.Posts.CountAsync(p => 
                p.ModerationStatus == ModerationStatus.Flagged || 
                p.ModerationStatus == ModerationStatus.AwaitingManualReview);
            
            var flaggedMessages = await _context.Messages.CountAsync(m => 
                m.ModerationStatus == ModerationStatus.Flagged || 
                m.ModerationStatus == ModerationStatus.AwaitingManualReview);
                
            var pendingReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending);
            var unconfirmedEmails = await _context.Users.CountAsync(u => !u.EmailConfirmed);

            ViewBag.PendingVerifications = pendingVerifications;
            ViewBag.FlaggedContentCount = flaggedPosts + flaggedMessages;
            ViewBag.PendingReportsCount = pendingReports;
            ViewBag.UnconfirmedEmailsCount = unconfirmedEmails;

            return View();
        }
    }
}
