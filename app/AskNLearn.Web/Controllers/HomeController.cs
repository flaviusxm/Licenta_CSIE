using System.Diagnostics;
using AskNLearn.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using System;
using System.Linq;
using AskNLearn.Domain.Entities.Core;
using AskNLearn.Application.Common.Models;

namespace app_licenta.Controllers
{
    public class HomeController(AskNLearn.Application.Common.Interfaces.IApplicationDbContext context) : Controller
    {
        private readonly AskNLearn.Application.Common.Interfaces.IApplicationDbContext _context = context;

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true && User.HasClaim(ClaimTypes.Role, "Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var recentPosts = await GetPosts(currentUserId, 0, 10);

            ViewBag.RecentPosts = recentPosts;
            ViewBag.CurrentUserId = currentUserId;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetFeed(int skip = 10)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var posts = await GetPosts(currentUserId, skip, 10);
            
            ViewBag.CurrentUserId = currentUserId;
            return PartialView("_PostCards", posts);
        }

        private async Task<List<HomeFeedPostDto>> GetPosts(string? currentUserId, int skip, int take)
        {
            return await _context.Posts
                .Where(p => p.CommunityId != null && p.ModerationStatus != ModerationStatus.Flagged)
                .Include(p => p.Author)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Join(_context.Communities,
                    p => p.CommunityId,
                    c => (Guid?)c.Id,
                    (p, c) => new { p, c })
                .Select(x => new HomeFeedPostDto
                {
                    Id = x.p.Id,
                    CommunityId = x.p.CommunityId!.Value,
                    CommunityName = x.c.Name,
                    AuthorId = x.p.AuthorId,
                    AuthorName = x.p.Author != null ? x.p.Author.FullName : "Unknown",
                    AuthorConnectionStatus = string.IsNullOrEmpty(currentUserId) ? ConnectionStatus.None : 
                        _context.Friendships.Any(f => (f.RequesterId == currentUserId && f.AddresseeId == x.p.AuthorId && f.Status == FriendshipStatus.Accepted) || 
                                                     (f.RequesterId == x.p.AuthorId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Accepted)) ? ConnectionStatus.Accepted :
                        _context.Friendships.Any(f => f.RequesterId == currentUserId && f.AddresseeId == x.p.AuthorId && f.Status == FriendshipStatus.Pending) ? ConnectionStatus.PendingSent :
                        _context.Friendships.Any(f => f.RequesterId == x.p.AuthorId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Pending) ? ConnectionStatus.PendingReceived : ConnectionStatus.None,
                    Title = x.p.Title,
                    Content = x.p.Content,
                    CommentCount = x.p.Comments.Count,
                    ViewCount = x.p.ViewCount,
                    VoteCount = _context.PostVotes.Where(v => v.PostId == x.p.Id).Select(v => (int)v.VoteValue).Sum(),
                    IsSolved = x.p.IsSolved,
                    CreatedAt = x.p.CreatedAt
                })
                .ToListAsync();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class HomeFeedPostDto
    {
        public Guid Id { get; set; }
        public Guid CommunityId { get; set; }
        public string CommunityName { get; set; } = "";
        public string? AuthorId { get; set; }
        public string AuthorName { get; set; } = "";
        public ConnectionStatus AuthorConnectionStatus { get; set; } = ConnectionStatus.None;
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public int CommentCount { get; set; }
        public int ViewCount { get; set; }
        public int VoteCount { get; set; }
        public bool IsSolved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
