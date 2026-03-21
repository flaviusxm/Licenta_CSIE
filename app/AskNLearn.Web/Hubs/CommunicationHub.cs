using Microsoft.AspNetCore.SignalR;
using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AskNLearn.Web.Hubs
{
    public class CommunicationHub(IApplicationDbContext context) : Hub
    {
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
                ChannelId = channelId,
                Content = content,
                AuthorId = userId,
                CreatedAt = DateTime.UtcNow
            };

            context.Messages.Add(message);
            await context.SaveChangesAsync(default);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            await Clients.Group(channelId.ToString()).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                channelId = channelId,
                content = message.Content,
                authorId = userId,
                authorName = user?.FullName ?? user?.UserName ?? "Unknown",
                authorAvatar = user?.AvatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={user?.UserName}",
                createdAt = message.CreatedAt.ToString("HH:mm")
            });
        }

        // WebRTC Signaling
        public async Task SendSignal(string signal, string targetConnectionId)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveSignal", signal, Context.ConnectionId);
        }

        public async Task JoinVoice(Guid channelId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.Identity?.Name ?? "Unknown";
            
            await Groups.AddToGroupAsync(Context.ConnectionId, $"voice-{channelId}");
            
            // Notify others in the voice channel
            await Clients.OthersInGroup($"voice-{channelId}").SendAsync("UserJoinedVoice", new
            {
                connectionId = Context.ConnectionId,
                userId = userId,
                userName = userName,
                channelId = channelId
            });
        }

        public async Task LeaveVoice(Guid channelId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"voice-{channelId}");
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
