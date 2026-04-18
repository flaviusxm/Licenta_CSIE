using AskNLearn.Domain.Entities.Core;
using AskNLearn.Infrastructure.Persistance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Domain.Entities.Messaging;
using MediatR;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    [Route("identity/network")]
    public class ConnectionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        [HttpPost("/Connections/SendRequest")]
        public async Task<IActionResult> SendRequest([FromQuery] string userId)
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
            await context.SaveChangesAsync();
            return Ok(new { status = "PendingSent" });
        }

        [HttpPost("/Connections/AcceptRequest")]
        public async Task<IActionResult> AcceptRequest([FromQuery] string userId)
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

        [HttpPost("/Connections/DeclineRequest")]
        public async Task<IActionResult> DeclineRequest([FromQuery] string userId)
        {
            var currentUserId = userManager.GetUserId(User);
            var request = await context.Friendships
                .FirstOrDefaultAsync(f => f.RequesterId == userId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Pending);

            if (request == null) return NotFound();

            context.Friendships.Remove(request);
            await context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("/Connections/RemoveConnection")]
        public async Task<IActionResult> RemoveConnection([FromQuery] string userId)
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

        [HttpGet("/Connections/SearchPeople")]
        public async Task<IActionResult> SearchPeople(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Ok(new List<object>());
            
            var users = await userManager.Users
                .Where(u => u.UserName!.Contains(term) || u.FullName!.Contains(term))
                .Take(10)
                .Select(u => new 
                {
                    id = u.Id,
                    userName = u.UserName,
                    fullName = u.FullName,
                    avatarUrl = u.AvatarUrl
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}
