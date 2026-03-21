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

        private async Task SetSidebarStats()
        {
            ViewBag.PendingVerifications = await _context.VerificationRequests.CountAsync(v => v.Status == Status.Pending);
            var flaggedPosts = await _context.Posts.CountAsync(p => p.ModerationStatus == ModerationStatus.Flagged);
            var flaggedMessages = await _context.Messages.CountAsync(m => m.ModerationStatus == ModerationStatus.Flagged);
            ViewBag.FlaggedContentCount = flaggedPosts + flaggedMessages;
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
            ViewBag.TotalCommunities = await _context.Communities.CountAsync();
            
            await SetSidebarStats();
            
            return View();
        }

        public async Task<IActionResult> Verifications()
        {
            if (!await IsAdmin())
            {
                TempData["ErrorMessage"] = "Access Denied: You do not have the Admin role.";
                return RedirectToAction("Index", "Home");
            }

            await SetSidebarStats();

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

            var posts = await _context.Posts
                .Include(p => p.Author)
                .OrderByDescending(p => p.ModerationStatus == ModerationStatus.Flagged)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            var messages = await _context.Messages
                .Include(m => m.Author)
                .Include(m => m.Post)
                .OrderByDescending(m => m.ModerationStatus == ModerationStatus.Flagged)
                .ThenByDescending(m => m.CreatedAt)
                .ToListAsync();

            ViewBag.Posts = posts;
            ViewBag.Messages = messages;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ApprovePost(Guid id)
        {
            if (!await IsAdmin()) return Forbid();
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            post.ModerationStatus = ModerationStatus.Approved;
            await _context.SaveChangesAsync(default);
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
