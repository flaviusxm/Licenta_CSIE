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

            var isChannel = await context.Channels.AnyAsync(c => c.Id == channelId);
            if (isChannel) message.ChannelId = channelId;
            else message.ConversationId = channelId;

            context.Messages.Add(message);

            // Update last read for sender
            if (!isChannel)
            {
                var senderParticipant = await context.DirectConversationParticipants
                    .FirstOrDefaultAsync(p => p.ConversationId == channelId && p.UserId == userId);
                if (senderParticipant != null)
                {
                    senderParticipant.LastReadMessageId = message.Id;
                }
            }
            
            await context.SaveChangesAsync(default);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            await Clients.Group(channelId.ToString()).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                channelId = isChannel ? channelId : (Guid?)null,
                conversationId = !isChannel ? channelId : (Guid?)null,
                content = message.Content,
                authorId = userId,
                authorName = user?.FullName ?? user?.UserName ?? "Unknown",
                authorAvatar = user?.AvatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={user?.UserName}",
                createdAt = message.CreatedAt.ToString("HH:mm")
            });
        }

        // WebRTC Signaling
        public async Task StartCall(string targetUserId, Guid conversationId, string type)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            await Clients.Group($"user-{targetUserId}").SendAsync("IncomingCall", new
            {
                fromUserId = userId,
                fromUserName = user?.FullName ?? user?.UserName ?? "Someone",
                conversationId = conversationId,
                type = type // 'voice' or 'video'
            });
        }

        public async Task EndCall(string targetUserId, Guid conversationId)
        {
            await Clients.Group($"user-{targetUserId}").SendAsync("CallEnded", conversationId);
        }

        public async Task SendSignal(string signal, string targetConnectionId)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveSignal", signal, Context.ConnectionId);
        }

        public async Task JoinVoice(Guid channelId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return;
            var userName = Context.User?.Identity?.Name ?? "Unknown";
            
            await Groups.AddToGroupAsync(Context.ConnectionId, $"voice-{channelId}");
            await tracker.JoinVoiceChannel(channelId, userId, Context.ConnectionId);

            // Fetch existing users in the channel to notify the joiner
            var existingUsers = await tracker.GetUsersInVoiceChannel(channelId);
            
            // Notify others in the voice channel
            await Clients.OthersInGroup($"voice-{channelId}").SendAsync("UserJoinedVoice", new
            {
                connectionId = Context.ConnectionId,
                userId = userId,
                userName = userName,
                channelId = channelId
            });

            // Send existing users to the joiner (excluding self)
            foreach (var user in existingUsers.Where(u => u.ConnectionId != Context.ConnectionId))
            {
                var dbUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.UserId);
                await Clients.Caller.SendAsync("UserJoinedVoice", new
                {
                    connectionId = user.ConnectionId,
                    userId = user.UserId,
                    userName = dbUser?.UserName ?? "User",
                    channelId = channelId
                });
            }
        }

        public async Task LeaveVoice(Guid channelId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"voice-{channelId}");
            await tracker.LeaveVoiceChannel(channelId, Context.ConnectionId);
            await Clients.Group($"voice-{channelId}").SendAsync("UserLeftVoice", Context.ConnectionId);
        }

        public async Task ToggleMute(Guid channelId, bool isMuted)
        {
            await Clients.Group($"voice-{channelId}").SendAsync("UserMuteChanged", Context.ConnectionId, isMuted);
        }

        public async Task ToggleVideo(Guid channelId, bool isVideoOn)
        {
            await Clients.Group($"voice-{channelId}").SendAsync("UserVideoChanged", Context.ConnectionId, isVideoOn);
        }

        public async Task ToggleSpeaking(Guid channelId, bool isSpeaking)
        {
            await Clients.Group($"voice-{channelId}").SendAsync("UserSpeakingChanged", Context.ConnectionId, isSpeaking);
        }
    }
}
