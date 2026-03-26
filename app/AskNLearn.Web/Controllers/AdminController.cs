using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Core;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(IApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<bool> IsAdmin()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;
            return user.Role == Role.Admin;
        }

        public async Task<IActionResult> Index()
        {
            if (!await IsAdmin())
            {
                TempData["ErrorMessage"] = "Access Denied: You do not have the Admin role.";
                return RedirectToAction("Index", "Home");
            }
            
            // Statistics for dashboard
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.PendingVerifications = await _context.VerificationRequests.CountAsync(v => v.Status == Status.Pending);
            ViewBag.TotalCommunities = await _context.Communities.CountAsync();
            
            // Flagged content statistics (AI + User Reports)
            var flaggedPosts = await _context.Posts.CountAsync(p => p.ModerationStatus == ModerationStatus.Flagged || p.ModerationStatus == ModerationStatus.Pending);
            var flaggedMessages = await _context.Messages.CountAsync(m => m.ModerationStatus == ModerationStatus.Flagged || m.ModerationStatus == ModerationStatus.Pending);
            var pendingReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending);
            ViewBag.FlaggedContentCount = flaggedPosts + flaggedMessages;
            ViewBag.PendingReportsCount = pendingReports;
            
            return View();
        }

        public async Task<IActionResult> Verifications()
        {
            if (!await IsAdmin())
            {
                TempData["ErrorMessage"] = "Access Denied: You do not have the Admin role.";
                return RedirectToAction("Index", "Home");
            }

            var requests = await _context.VerificationRequests
                .Include(v => v.User)
                .OrderByDescending(v => v.SubmittedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(Guid id)
        {
            if (!await IsAdmin()) return Forbid();

            var request = await _context.VerificationRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = Status.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedBy = _userManager.GetUserId(User);

            var user = await _context.Users.FindAsync(request.UserId);
            if (user != null)
            {
                user.IsVerified = true;
            }

            await _context.SaveChangesAsync(default);
            return RedirectToAction(nameof(Verifications));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(Guid id, string notes)
        {
            if (!await IsAdmin()) return Forbid();

            var request = await _context.VerificationRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = Status.Rejected;
            request.AdminNotes = notes;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedBy = _userManager.GetUserId(User);

            await _context.SaveChangesAsync(default);
            return RedirectToAction(nameof(Verifications));
        }

        public async Task<IActionResult> Moderation()
        {
            if (!await IsAdmin())
            {
                TempData["ErrorMessage"] = "Access Denied: You do not have the Admin role.";
                return RedirectToAction("Index", "Home");
            }

            // Just send counts for the tabs
            ViewBag.FlaggedPostsCount = await _context.Posts.CountAsync(p => p.ModerationStatus == ModerationStatus.Flagged || p.ModerationStatus == ModerationStatus.Pending);
            ViewBag.FlaggedMessagesCount = await _context.Messages.CountAsync(m => m.ModerationStatus == ModerationStatus.Flagged || m.ModerationStatus == ModerationStatus.Pending);
            ViewBag.PendingReportsCount = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending);
            
            return View(new AskNLearn.Web.Models.ModerationViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> LoadAIReports(int skip = 0, int take = 20, string filter = "ALL")
        {
            if (!await IsAdmin()) return Forbid();

            var postsQuery = _context.Posts.AsNoTracking()
                .Where(p => p.ModerationStatus == ModerationStatus.Flagged || p.ModerationStatus == ModerationStatus.Pending);
            
            var messagesQuery = _context.Messages.AsNoTracking()
                .Where(m => m.ModerationStatus == ModerationStatus.Flagged || m.ModerationStatus == ModerationStatus.Pending);

            int totalCount = 0;
            var combinedList = new System.Collections.Generic.List<dynamic>();

            if (filter == "ALL" || filter == "POST")
            {
                totalCount += await postsQuery.CountAsync();
                var posts = await postsQuery
                    .Include(p => p.Author)
                    .Select(p => new { 
                        Id = p.Id, 
                        Title = p.Title, 
                        Content = p.Content, 
                        Author = p.Author.UserName, 
                        CreatedAt = p.CreatedAt, 
                        Type = "POST",
                        Reason = p.ModerationReason,
                        Status = p.ModerationStatus,
                        ParentTitle = (string)null
                    })
                    .OrderByDescending(p => p.Status == ModerationStatus.Flagged)
                    .ThenByDescending(p => p.CreatedAt)
                    .Skip(skip).Take(take)
                    .ToListAsync();
                combinedList.AddRange(posts);
            }

            if (filter == "ALL" || filter == "COMMENT")
            {
                totalCount += await messagesQuery.CountAsync();
                var messages = await messagesQuery
                    .Include(m => m.Author)
                    .Include(m => m.Post)
                    .Select(m => new { 
                        Id = m.Id, 
                        Title = (string)null, 
                        Content = m.Content, 
                        Author = m.Author.UserName, 
                        CreatedAt = m.CreatedAt, 
                        Type = "COMMENT",
                        Reason = m.ModerationReason,
                        Status = m.ModerationStatus,
                        ParentTitle = m.Post.Title
                    })
                    .OrderByDescending(m => m.Status == ModerationStatus.Flagged)
                    .ThenByDescending(m => m.CreatedAt)
                    .Skip(skip).Take(take)
                    .ToListAsync();
                combinedList.AddRange(messages);
            }

            Response.Headers.Add("X-Total-Count", totalCount.ToString());

            // Final sort and slice if ALL combined
            var finalResult = combinedList
                .OrderByDescending(x => (ModerationStatus)x.Status == ModerationStatus.Flagged)
                .ThenByDescending(x => (DateTime)x.CreatedAt)
                .Take(take)
                .ToList();

            return PartialView("_AIReportsTable", finalResult);
        }

        [HttpGet]
        public async Task<IActionResult> LoadUserReports(int skip = 0, int take = 20, string filter = "ALL")
        {
            if (!await IsAdmin()) return Forbid();

            var query = _context.Reports.AsNoTracking()
                .Include(r => r.Reporter)
                .Include(r => r.ReportedPost)
                .Include(r => r.ReportedMessage)
                .Where(r => r.Status == ReportStatus.Pending);

            if (filter == "POST")
                query = query.Where(r => r.ReportedPostId != null);
            else if (filter == "COMMENT")
                query = query.Where(r => r.ReportedMessageId != null);

            int totalCount = await query.CountAsync();
            Response.Headers.Add("X-Total-Count", totalCount.ToString());

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip).Take(take)
                .ToListAsync();

            return PartialView("_UserReportsTable", reports);
        }

        [HttpPost]
        public async Task<IActionResult> ApprovePost(Guid id)
        {
            if (!await IsAdmin()) return Forbid();
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            post.ModerationStatus = ModerationStatus.Approved;
            await _context.SaveChangesAsync(default);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok();
            return RedirectToAction(nameof(Moderation));
        }

        [HttpPost]
        public async Task<IActionResult> FlagPost(Guid id)
        {
            if (!await IsAdmin()) return Forbid();
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            post.ModerationStatus = ModerationStatus.Flagged;
            await _context.SaveChangesAsync(default);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok();
            return RedirectToAction(nameof(Moderation));
        }

        [HttpPost]
        public async Task<IActionResult> ApproveComment(Guid id)
        {
            if (!await IsAdmin()) return Forbid();
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();

            message.ModerationStatus = ModerationStatus.Approved;
            await _context.SaveChangesAsync(default);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok();
            return RedirectToAction(nameof(Moderation));
        }

        [HttpPost]
        public async Task<IActionResult> FlagComment(Guid id)
        {
            if (!await IsAdmin()) return Forbid();
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();

            message.ModerationStatus = ModerationStatus.Flagged;
            await _context.SaveChangesAsync(default);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok();
            return RedirectToAction(nameof(Moderation));
        }

        [HttpPost]
        public async Task<IActionResult> ResolveReport(Guid id)
        {
            if (!await IsAdmin()) return Forbid();
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = ReportStatus.Resolved;
            await _context.SaveChangesAsync(default);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok();
            return RedirectToAction(nameof(Moderation));
        }

        [HttpPost]
        public async Task<IActionResult> DismissReport(Guid id)
        {
            if (!await IsAdmin()) return Forbid();
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = ReportStatus.Dismissed;
            await _context.SaveChangesAsync(default);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Ok();
            return RedirectToAction(nameof(Moderation));
        }

        [AllowAnonymous]
        public IActionResult Diagnostic()
        {
            return Ok(new
            {
                Status = "Online",
                Authenticated = User.Identity?.IsAuthenticated,
                Claims = User.Claims.Select(c => new { c.Type, c.Value }),
                Time = DateTime.UtcNow
            });
        }
    }
}
