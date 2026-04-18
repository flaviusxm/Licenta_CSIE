using Microsoft.AspNetCore.SignalR;
using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AskNLearn.Web.Hubs
{
    public class CommunicationHub(IApplicationDbContext context, IPresenceTracker tracker) : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
                var isFirst = await tracker.UserConnected(userId, Context.ConnectionId);
                if (isFirst)
                {
                    await Clients.Others.SendAsync("UserIsOnline", userId);
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
                var isLast = await tracker.UserDisconnected(userId, Context.ConnectionId);
                if (isLast)
                {
                    await Clients.Others.SendAsync("UserIsOffline", userId);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task NotifyConnectionRequest(string targetUserId, string requesterName)
        {
            await Clients.Group($"user-{targetUserId}").SendAsync("ReceiveConnectionRequest", new
            {
                requesterName = requesterName,
                message = $"{requesterName} wants to connect with you!"
            });
        }

        public async Task NotifyConnectionAccepted(string targetUserId, string acceptorName)
        {
            await Clients.Group($"user-{targetUserId}").SendAsync("ReceiveConnectionAccepted", new
            {
                acceptorName = acceptorName,
                message = $"{acceptorName} accepted your connection request!"
            });
        }

        public async Task JoinChannel(Guid channelId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, channelId.ToString());
        }

        public async Task LeaveChannel(Guid channelId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId.ToString());
        }

        public async Task SendMessage(Guid channelId, string content)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return;

            var message = new Message
            {
                Content = content,
                AuthorId = userId,
                CreatedAt = DateTime.UtcNow
            };

            message.ConversationId = channelId;

            context.Messages.Add(message);

            // Update last read for sender
            var senderParticipant = await context.DirectConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == channelId && p.UserId == userId);
            if (senderParticipant != null)
            {
                senderParticipant.LastReadMessageId = message.Id;
            }
            
            await context.SaveChangesAsync(default);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            await Clients.Group(channelId.ToString()).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                conversationId = channelId,
                content = message.Content,
                authorId = userId,
                authorName = user?.FullName ?? user?.UserName ?? "Unknown",
                authorAvatar = user?.AvatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={user?.UserName}",
                createdAt = message.CreatedAt.ToString("HH:mm")
            });
        }

    }
}
