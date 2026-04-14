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
    public class DirectController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AskNLearn.Application.Common.Interfaces.IModerationQueue moderationQueue) : Controller
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
                var userParticipant = c.Participants.FirstOrDefault(p => p.UserId == user.Id);
                var lastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
                
                // Fetch unread count from database for this conversation
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
                    UnreadCount = unreadCount
                };
            }).ToList();

            ViewBag.Conversations = conversationList;

            if (id.HasValue)
            {
                var selectedConversation = await context.DirectConversations
                    .Include(c => c.Participants)
                        .ThenInclude(p => p.User)
                    .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                        .ThenInclude(m => m.Author)
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

        [HttpPost]
        public async Task<IActionResult> ReportMessage(Guid id, AskNLearn.Domain.Entities.Core.ReportReason reason, string description)
        {
            var userId = userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var message = await context.Messages.FindAsync(id);
            if (message == null) return NotFound();

            var report = new AskNLearn.Domain.Entities.Core.Report
            {
                ReporterId = userId!,
                ReportedMessageId = id,
                Reason = reason,
                Description = description ?? "No description provided",
                CreatedAt = DateTime.UtcNow,
                Status = AskNLearn.Domain.Entities.Core.ReportStatus.Pending
            };

            context.Reports.Add(report);
            await context.SaveChangesAsync();

            // Enqueue for AI Re-evaluation
            moderationQueue.Enqueue(new AskNLearn.Application.Common.Interfaces.ModerationTask
            {
                Id = report.Id,
                Content = message.Content ?? string.Empty,
                Target = AskNLearn.Application.Common.Interfaces.ModerationTarget.Report,
                Reason = reason
            });

            return Ok(new { message = "Message reported successfully. Guardian AI is analyzing." });
        }

        public IActionResult Messages() => RedirectToAction("Index");
    }
}
