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
    [Route("hubs/home")]
    public class HomeController(AskNLearn.Application.Common.Interfaces.IApplicationDbContext context) : Controller
    {
        private readonly AskNLearn.Application.Common.Interfaces.IApplicationDbContext _context = context;

        [HttpGet("/")]
        [HttpGet("")]
        public async Task<IActionResult> Index(string sortBy = "Latest")
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var recentPosts = await GetPosts(currentUserId, 0, 10, sortBy);

            // Fetch Top Communities for Sidebar (based on activity/posts)
            var topCommunities = await _context.Communities
                .Select(c => new 
                { 
                    c.Id, 
                    c.Name, 
                    PostCount = _context.Posts.Count(p => p.CommunityId == c.Id), 
                    c.ImageUrl 
                })
                .OrderByDescending(c => c.PostCount)
                .Take(5)
                .ToListAsync();

            // Fetch User Stats if logged in
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var user = await _context.Users
                    .Include(u => u.CurrentRank)
                    .FirstOrDefaultAsync(u => u.Id == currentUserId);
                
                if (user != null)
                {
                    // Calculate Global Notification Count
                    var unreadNotif = await _context.Notifications
                        .CountAsync(n => n.UserId == currentUserId && !n.IsRead);
                        
                    var pendingReq = await _context.Friendships
                        .CountAsync(f => f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Pending);
                    
                    var unreadMsgs = 0;
                    var participants = await _context.DirectConversationParticipants
                        .Where(p => p.UserId == currentUserId)
                        .ToListAsync();

                    foreach(var p in participants)
                    {
                        var lastReadAt = DateTime.MinValue;
                        if (p.LastReadMessageId != null)
                        {
                            lastReadAt = await _context.Messages
                                .Where(m => m.Id == p.LastReadMessageId)
                                .Select(m => m.CreatedAt)
                                .FirstOrDefaultAsync();
                        }
                        
                        unreadMsgs += await _context.Messages
                            .CountAsync(m => m.ConversationId == p.ConversationId && m.AuthorId != currentUserId && m.CreatedAt > lastReadAt);
                    }

                    ViewBag.UserStats = new 
                    { 
                        user.FullName, 
                        Reputation = user.ReputationPoints, 
                        user.AvatarUrl, 
                        RankName = user.CurrentRank?.Name ?? "Student",
                        NotificationCount = unreadNotif + pendingReq + unreadMsgs
                    };
                }
            }

            ViewBag.RecentPosts = recentPosts;
            ViewBag.TopCommunities = topCommunities;
            ViewBag.AllCommunities = await _context.Communities.Select(c => new { c.Id, c.Name }).ToListAsync();
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.SortBy = sortBy;
            return View();
        }

        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed(int skip = 10, string sortBy = "Latest")
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var posts = await GetPosts(currentUserId, skip, 10, sortBy);
            
            ViewBag.CurrentUserId = currentUserId;
            return PartialView("_PostCards", posts);
        }

        private async Task<List<HomeFeedPostDto>> GetPosts(string? currentUserId, int skip, int take, string sortBy = "Latest")
        {
            var query = _context.Posts
                .Where(p => p.CommunityId != null && p.ModerationStatus != ModerationStatus.Flagged);

            query = sortBy switch
            {
                "TopRated" => query.OrderByDescending(p => p.Votes.Sum(v => (int)v.VoteValue)),
                "Latest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            return await query
                .Skip(skip)
                .Take(take)
                .Select(p => new HomeFeedPostDto
                {
                    Id = p.Id,
                    CommunityId = p.CommunityId ?? Guid.Empty,
                    CommunityName = _context.Communities.Where(c => c.Id == p.CommunityId).Select(c => c.Name).FirstOrDefault() ?? "Unknown Community",
                    AuthorId = p.AuthorId,
                    AuthorName = p.Author != null ? p.Author.FullName : "Unknown Student",
                    AuthorConnectionStatus = string.IsNullOrEmpty(currentUserId) ? ConnectionStatus.None : 
                        _context.Friendships.Any(f => (f.RequesterId == currentUserId && f.AddresseeId == p.AuthorId && f.Status == FriendshipStatus.Accepted) || 
                                                     (f.RequesterId == p.AuthorId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Accepted)) ? ConnectionStatus.Accepted :
                        _context.Friendships.Any(f => f.RequesterId == currentUserId && f.AddresseeId == p.AuthorId && f.Status == FriendshipStatus.Pending) ? ConnectionStatus.PendingSent :
                        _context.Friendships.Any(f => f.RequesterId == p.AuthorId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Pending) ? ConnectionStatus.PendingReceived : ConnectionStatus.None,
                    Title = p.Title,
                    Content = p.Content,
                    CommentCount = p.Comments.Count,
                    ViewCount = p.ViewCount,
                    VoteCount = _context.PostVotes.Where(v => v.PostId == p.Id).Select(v => (int)v.VoteValue).Sum(),
                    IsSolved = p.IsSolved,
                    CreatedAt = p.CreatedAt,
                    UserVote = !string.IsNullOrEmpty(currentUserId)
                        ? _context.PostVotes.Where(v => v.PostId == p.Id && v.UserId == currentUserId).Select(v => (int)v.VoteValue).FirstOrDefault()
                        : 0
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
        public int UserVote { get; set; } // 1, -1, or 0
        public DateTime CreatedAt { get; set; }
    }
}
