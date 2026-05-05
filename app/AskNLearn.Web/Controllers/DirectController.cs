using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Infrastructure.Persistance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    [Route("communication/messaging")]
    public class DirectController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AskNLearn.Application.Common.Interfaces.IModerationQueue moderationQueue) : Controller
    {
        [HttpGet("conversations/{id?}")]
        public IActionResult Index(Guid? id)
        {
            // Redirect to the Unified Communication Hub in InboxController
            return RedirectToAction("Index", "Inbox", new { id = id });
        }

        [HttpPost("conversations/initialize")]
        public async Task<IActionResult> StartChat(string userId)
        {
            var currentUserId = userManager.GetUserId(User);
            if (currentUserId == userId) return BadRequest("Cannot chat with yourself.");

            // var isConnected = await context.Friendships.AnyAsync(f => 
            //     ((f.RequesterId == currentUserId && f.AddresseeId == userId) || 
            //      (f.RequesterId == userId && f.AddresseeId == currentUserId)) && 
            //     f.Status == FriendshipStatus.Accepted);
            // 
            // if (!isConnected) return BadRequest("You must be connected to start a chat.");

            var conversation = await context.DirectConversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Participants.Any(p => p.UserId == currentUserId) && 
                                          c.Participants.Any(p => p.UserId == userId));

            if (conversation == null)
            {
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

            return RedirectToAction("Index", "Inbox", new { id = conversation.Id });
        }

        [HttpPost("messages/report")]
        public async Task<IActionResult> ReportMessage(Guid id, ReportReason reason, string description)
        {
            return BadRequest("Direct messages cannot be reported in this version.");
        }

        [HttpPost("messages/delete")]
        public async Task<IActionResult> DeleteMessage(Guid id)
        {
            var userId = userManager.GetUserId(User);
            var message = await context.Messages.FindAsync(id);
            if (message == null) return NotFound();
            if (message.AuthorId != userId) return Forbid();

            context.Messages.Remove(message);
            await context.SaveChangesAsync();
            return Ok();
        }
    }
}
