using AskNLearn.Domain.Entities.Core;
using AskNLearn.Infrastructure.Persistance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Domain.Entities.Messaging;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    public class ConnectionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        [HttpPost]
        public async Task<IActionResult> SendRequest(string userId)
        {
            var currentUserId = userManager.GetUserId(User);
            if (currentUserId == userId) return BadRequest("You cannot connect with yourself.");

            var existing = await context.Friendships
                .FirstOrDefaultAsync(f => (f.RequesterId == currentUserId && f.AddresseeId == userId) || 
                                          (f.RequesterId == userId && f.AddresseeId == currentUserId));

            if (existing != null) return BadRequest("A request or connection already exists.");

            var friendship = new Friendship
            {
                RequesterId = currentUserId!,
                AddresseeId = userId,
                Status = FriendshipStatus.Pending
            };

            context.Friendships.Add(friendship);
            
            // Add notification
            var notification = new Notification
            {
                UserId = userId,
                Title = "New Connection Request",
                Message = $"{User.Identity?.Name} wants to connect with you.",
                CreatedAt = DateTime.UtcNow
            };
            context.Notifications.Add(notification);

            await context.SaveChangesAsync();
            return Ok(new { status = "PendingSent" });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest(string userId)
        {
            var currentUserId = userManager.GetUserId(User);
            var request = await context.Friendships
                .FirstOrDefaultAsync(f => f.RequesterId == userId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Pending);

            if (request == null) return NotFound();

            request.Status = FriendshipStatus.Accepted;

            // Notify requester
            var notification = new Notification
            {
                UserId = userId,
                Title = "Connection Accepted",
                Message = $"{User.Identity?.Name} accepted your connection request.",
                CreatedAt = DateTime.UtcNow
            };
            context.Notifications.Add(notification);

            await context.SaveChangesAsync();
            return Ok(new { status = "Accepted" });
        }

        [HttpPost]
        public async Task<IActionResult> DeclineRequest(string userId)
        {
            var currentUserId = userManager.GetUserId(User);
            var request = await context.Friendships
                .FirstOrDefaultAsync(f => f.RequesterId == userId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Pending);

            if (request == null) return NotFound();

            context.Friendships.Remove(request);
            await context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RemoveConnection(string userId)
        {
            var currentUserId = userManager.GetUserId(User);
            var friendship = await context.Friendships
                .FirstOrDefaultAsync(f => (f.RequesterId == currentUserId && f.AddresseeId == userId) || 
                                          (f.RequesterId == userId && f.AddresseeId == currentUserId));

            if (friendship == null) return NotFound();

            context.Friendships.Remove(friendship);
            await context.SaveChangesAsync();
            return Ok();
        }
    }
}
