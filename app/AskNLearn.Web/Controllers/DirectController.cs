using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Infrastructure.Persistance;
using AskNLearn.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    public class DirectController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        public async Task<IActionResult> Index(Guid? id)
        {
            ViewData["ActivePage"] = "Messages";
            var user = await userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("SignIn", "Auth");

            var conversations = await context.DirectConversations
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(c => c.Participants.Any(p => p.UserId == user.Id))
                .OrderByDescending(c => c.Messages.Any() ? c.Messages.Max(m => m.CreatedAt) : c.CreatedAt)
                .ToListAsync();

            var conversationList = conversations.Select(c => {
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

            ViewBag.Conversations = conversationList;

            if (id.HasValue)
            {
                var selectedConversation = await context.DirectConversations
                    .Include(c => c.Participants)
                        .ThenInclude(p => p.User)
                    .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                    .FirstOrDefaultAsync(c => c.Id == id.Value && c.Participants.Any(p => p.UserId == user.Id));

                if (selectedConversation != null)
                {
                    // Update last read message
                    var userParticipant = selectedConversation.Participants.FirstOrDefault(p => p.UserId == user.Id);
                    var lastMessage = selectedConversation.Messages.LastOrDefault();
                    if (userParticipant != null && lastMessage != null)
                    {
                        userParticipant.LastReadMessageId = lastMessage.Id;
                        await context.SaveChangesAsync();
                    }

                    return View(selectedConversation);
                }
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> StartChat(string userId)
        {
            var currentUserId = userManager.GetUserId(User);
            if (currentUserId == userId) return BadRequest("Cannot chat with yourself.");

            // Check if connection exists and is accepted
            var isConnected = await context.Friendships.AnyAsync(f => 
                ((f.RequesterId == currentUserId && f.AddresseeId == userId) || 
                 (f.RequesterId == userId && f.AddresseeId == currentUserId)) && 
                f.Status == FriendshipStatus.Accepted);

            if (!isConnected) return BadRequest("You must be connected to start a chat.");

            // Find existing conversation
            var conversation = await context.DirectConversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Participants.Any(p => p.UserId == currentUserId) && 
                                          c.Participants.Any(p => p.UserId == userId));

            if (conversation == null)
            {
                // Create new conversation
                conversation = new DirectConversation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                };

                context.DirectConversations.Add(conversation);
                
                context.DirectConversationParticipants.Add(new DirectConversationParticipant { ConversationId = conversation.Id, UserId = currentUserId! });
                context.DirectConversationParticipants.Add(new DirectConversationParticipant { ConversationId = conversation.Id, UserId = userId });

                await context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { id = conversation.Id });
        }

        public IActionResult Messages() => RedirectToAction("Index");
    }
}
