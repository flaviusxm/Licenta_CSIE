using AskNLearn.Domain.Entities.Core;
using AskNLearn.Infrastructure.Persistance;
using AskNLearn.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModerationStatus = AskNLearn.Domain.Entities.Core.ModerationStatus;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    [Route("communication/notifications")]
    public class InboxController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        [HttpGet("overview/{id:guid?}")]
        public async Task<IActionResult> Index(Guid? id, [FromQuery] Guid? conversationId)
        {
            if (!id.HasValue && conversationId.HasValue) id = conversationId;
            ViewData["ActivePage"] = "Inbox";
            ViewData["AppLayout"] = true;
            var user = await userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("SignIn", "Auth");

            // 1. Fetch Direct Conversations
            var conversations = await context.DirectConversations
                .Include(c => c.Participants).ThenInclude(p => p.User)
                .Where(c => c.Participants.Any(p => p.UserId == user.Id))
                .ToListAsync();

            var messagePreviews = conversations.Select(c => {
                var otherParticipant = c.Participants.FirstOrDefault(p => p.UserId != user.Id);
                var userParticipant = c.Participants.FirstOrDefault(p => p.UserId == user.Id);
                
                // Fetch last SAFE message for preview
                var lastMessage = context.Messages
                    .Where(m => m.ConversationId == c.Id && 
                                m.ModerationStatus != ModerationStatus.Flagged && 
                                m.ModerationStatus != ModerationStatus.Removed)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();
                
                var unreadCount = context.Messages.Count(m => 
                    m.ConversationId == c.Id && 
                    m.AuthorId != user.Id && 
                    (userParticipant == null || userParticipant.LastReadMessageId == null || 
                     m.CreatedAt > (context.Messages.Where(lm => lm.Id == userParticipant.LastReadMessageId).Select(lm => lm.CreatedAt).FirstOrDefault())));

                return new ConversationPreviewViewModel
                {
                    ConversationId = c.Id,
                    OtherUserId = otherParticipant?.UserId ?? string.Empty,
                    OtherUserName = otherParticipant?.User?.FullName ?? otherParticipant?.User?.UserName ?? "Unknown User",
                    OtherUserAvatar = otherParticipant?.User?.AvatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={otherParticipant?.User?.UserName ?? "User"}",
                    LastMessageContent = lastMessage?.Content ?? "No messages yet",
                    LastMessageAt = lastMessage?.CreatedAt ?? c.CreatedAt,
                    IsUnread = unreadCount > 0,
                    UnreadCount = unreadCount,
                    IsChannel = false
                };
            }).OrderByDescending(c => c.LastMessageAt).ToList();

            // 2. Fetch Study Group Channels
            var groupIds = await context.GroupMemberships
                .Where(m => m.UserId == user.Id)
                .Select(m => m.GroupId)
                .ToListAsync();

            var channels = await context.Channels
                .Include(c => c.Group)
                .Where(c => groupIds.Contains(c.GroupId) && c.Type == AskNLearn.Domain.Entities.StudyGroup.ChannelType.Text)
                .ToListAsync();

            var channelPreviews = channels.Select(c => {
                var lastMessage = context.Messages
                    .Where(m => m.ChannelId == c.Id && 
                                m.ModerationStatus != ModerationStatus.Flagged && 
                                m.ModerationStatus != ModerationStatus.Removed)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();

                return new ConversationPreviewViewModel
                {
                    ConversationId = c.Id, // Reuse for routing
                    IsChannel = true,
                    ChannelId = c.Id,
                    GroupName = c.Group?.Name,
                    OtherUserName = c.Name,
                    LastMessageContent = lastMessage?.Content ?? "Welcome to the channel!",
                    LastMessageAt = lastMessage?.CreatedAt ?? DateTime.MinValue,
                    OtherUserAvatar = $"https://api.dicebear.com/7.x/initials/svg?seed={c.Name}"
                };
            }).OrderByDescending(c => c.LastMessageAt).ToList();

            // 3. Resolve Selected Content
            AskNLearn.Domain.Entities.Messaging.DirectConversation? selectedConversation = null;
            AskNLearn.Domain.Entities.StudyGroup.Channel? selectedChannel = null;

            if (id.HasValue)
            {
                // Check if it's a conversation
                selectedConversation = await context.DirectConversations
                    .Include(c => c.Participants).ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(c => c.Id == id.Value && c.Participants.Any(p => p.UserId == user.Id));

                if (selectedConversation != null)
                {
                    // Force load messages with a fresh query to avoid Include issues with tracking
                    var messages = await context.Messages
                        .Include(m => m.Author)
                        .Where(m => m.ConversationId == selectedConversation.Id && 
                                    m.ModerationStatus != ModerationStatus.Flagged && 
                                    m.ModerationStatus != ModerationStatus.Removed)
                        .OrderBy(m => m.CreatedAt)
                        .ToListAsync();
                    
                    selectedConversation.Messages = messages;
                }

                if (selectedConversation == null)
                {
                    // Check if it's a channel
                    selectedChannel = await context.Channels
                        .Include(c => c.Group)
                        .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                            .ThenInclude(m => m.Author)
                        .FirstOrDefaultAsync(c => c.Id == id.Value && groupIds.Contains(c.GroupId));

                    if (selectedChannel != null)
                    {
                        var messages = await context.Messages
                            .Include(m => m.Author)
                            .Where(m => m.ChannelId == selectedChannel.Id && 
                                        m.ModerationStatus != ModerationStatus.Flagged && 
                                        m.ModerationStatus != ModerationStatus.Removed)
                            .OrderBy(m => m.CreatedAt)
                            .ToListAsync();
                        
                        selectedChannel.Messages = messages;
                    }
                }
                else
                {
                    // Update last read for DM
                    var userParticipant = selectedConversation.Participants.FirstOrDefault(p => p.UserId == user.Id);
                    var lastMessage = selectedConversation.Messages.LastOrDefault();
                    if (userParticipant != null && lastMessage != null)
                    {
                        userParticipant.LastReadMessageId = lastMessage.Id;
                        await context.SaveChangesAsync();
                    }
                }
            }

            // 4. Notifications & Sidebar Stats
            var notifications = await context.Notifications.Where(n => n.UserId == user.Id).OrderByDescending(n => n.CreatedAt).ToListAsync();
            var pendingRequests = await context.Friendships.Include(f => f.Requester).Where(f => f.AddresseeId == user.Id && f.Status == FriendshipStatus.Pending).ToListAsync();
            var totalConnections = await context.Friendships.CountAsync(f => (f.RequesterId == user.Id || f.AddresseeId == user.Id) && f.Status == FriendshipStatus.Accepted);
            
            var connections = await context.Friendships
                .Include(f => f.Requester).Include(f => f.Addressee)
                .Where(f => (f.RequesterId == user.Id || f.AddresseeId == user.Id) && f.Status == FriendshipStatus.Accepted)
                .Select(f => f.RequesterId == user.Id ? f.Addressee : f.Requester).ToListAsync();

            var viewModel = new InboxViewModel
            {
                RecentMessages = messagePreviews,
                RecentChannels = channelPreviews,
                RecentNotifications = notifications,
                PendingRequests = pendingRequests,
                Connections = connections!,
                TotalConnections = totalConnections,
                SelectedConversation = selectedConversation,
                SelectedChannel = selectedChannel
            };

            return View(viewModel);
        }

        [HttpPost("v1/notifications/mark-read/{id:guid}")]
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

        [HttpPost("v1/notifications/mark-all-read")]
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

        [HttpGet("network")]
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
