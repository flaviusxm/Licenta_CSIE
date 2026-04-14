using AskNLearn.Domain.Entities.Core;
using AskNLearn.Infrastructure.Persistance;
using AskNLearn.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    public class InboxController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            ViewData["ActivePage"] = "Inbox";
            var user = await userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("SignIn", "Auth");

            // Fetch DMs
            var conversations = await context.DirectConversations
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(c => c.Participants.Any(p => p.UserId == user.Id))
                .OrderByDescending(c => c.Messages.Any() ? c.Messages.Max(m => m.CreatedAt) : c.CreatedAt)
                .ToListAsync();

            var messagePreviews = conversations.Select(c => {
                var otherParticipant = c.Participants.FirstOrDefault(p => p.UserId != user.Id);
                var lastMessage = c.Messages.FirstOrDefault();
                var userParticipant = c.Participants.FirstOrDefault(p => p.UserId == user.Id);

                return new ConversationPreviewViewModel
                {
                    ConversationId = c.Id,
                    OtherUserId = otherParticipant?.UserId ?? string.Empty,
                    OtherUserName = otherParticipant?.User?.FullName ?? otherParticipant?.User?.UserName ?? "Unknown User",
                    OtherUserAvatar = otherParticipant?.User?.AvatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={otherParticipant?.User?.UserName ?? "User"}",
                    LastMessageContent = lastMessage?.Content ?? "No messages yet",
                    LastMessageAt = lastMessage?.CreatedAt ?? c.CreatedAt,
                    IsUnread = lastMessage != null && userParticipant?.LastReadMessageId != lastMessage.Id && lastMessage.AuthorId != user.Id
                };
            }).ToList();

            // Fetch All Notifications
            var notifications = await context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Fetch Pending Friend Requests
            var pendingRequests = await context.Friendships
                .Include(f => f.Requester)
                .Where(f => f.AddresseeId == user.Id && f.Status == FriendshipStatus.Pending)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            // Fetch Total Connections (Accepted)
            var totalConnections = await context.Friendships
                .CountAsync(f => (f.RequesterId == user.Id || f.AddresseeId == user.Id) && f.Status == FriendshipStatus.Accepted);

            // Calculate Flow State
            // Logic: Peaking (Active < 24h & high interactions), Active (< 48h), Stable (default), Idle (> 7 days)
            var last24hActivities = await context.Messages.CountAsync(m => m.AuthorId == user.Id && m.CreatedAt > DateTime.UtcNow.AddDays(-1)) +
                                    await context.Notifications.CountAsync(n => n.UserId == user.Id && n.CreatedAt > DateTime.UtcNow.AddDays(-1));
            
            var last48hActivities = await context.Messages.CountAsync(m => m.AuthorId == user.Id && m.CreatedAt > DateTime.UtcNow.AddDays(-2));

            string flowState = "Stable";
            if (last24hActivities > 10) flowState = "Peaking";
            else if (last24hActivities > 0 || last48hActivities > 0) flowState = "Active";
            else if (user.LastActive < DateTime.UtcNow.AddDays(-7)) flowState = "Idle";
            
            var viewModel = new InboxViewModel
            {
                RecentMessages = messagePreviews,
                RecentNotifications = notifications,
                PendingRequests = pendingRequests,
                TotalConnections = totalConnections,
                FlowState = flowState,
                TotalUnreadCount = messagePreviews.Count(c => c.IsUnread) + 
                                  notifications.Count(n => !n.IsRead) +
                                  pendingRequests.Count
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var notification = await context.Notifications.FindAsync(id);
            if (notification != null && notification.UserId == userManager.GetUserId(User))
            {
                notification.IsRead = true;
                await context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = userManager.GetUserId(User);
            var unread = await context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread) n.IsRead = true;
            await context.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> Connections()
        {
            ViewData["ActivePage"] = "Connections";
            var userId = userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var friends = await context.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted)
                .Select(f => f.RequesterId == userId ? f.Addressee : f.Requester)
                .ToListAsync();

            return View(friends);
        }
    }
}
