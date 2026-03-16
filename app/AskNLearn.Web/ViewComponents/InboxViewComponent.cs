using AskNLearn.Domain.Entities.Core;
using AskNLearn.Infrastructure.Persistance;
using AskNLearn.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Web.ViewComponents
{
    public class InboxViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await userManager.GetUserAsync((System.Security.Claims.ClaimsPrincipal)User);
            if (user == null) return Content(string.Empty);

            // Fetch DMs
            var conversations = await context.DirectConversations
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(c => c.Participants.Any(p => p.UserId == user.Id))
                .OrderByDescending(c => c.Messages.Any() ? c.Messages.Max(m => m.CreatedAt) : c.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentMessages = conversations.Select(c => {
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

            // Fetch Notifications
            var notifications = await context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            var viewModel = new InboxViewModel
            {
                RecentMessages = recentMessages,
                RecentNotifications = notifications,
                TotalUnreadCount = recentMessages.Count(c => c.IsUnread) + notifications.Count(n => !n.IsRead)
            };

            return View(viewModel);
        }
    }
}
