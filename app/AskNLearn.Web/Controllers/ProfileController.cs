using AskNLearn.Application.Features.Users.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Application.Features.Users.Commands.SubmitVerificationRequest;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Domain.Entities.Core;
using Microsoft.AspNetCore.Identity;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    [Route("identity/profiles")]
    public class ProfileController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IFileService _fileService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(IMediator mediator, IFileService fileService, UserManager<ApplicationUser> userManager)
        {
            _mediator = mediator;
            _fileService = fileService;
            _userManager = userManager;
        }

        [HttpGet("view/{id?}")]
        public async Task<IActionResult> Index(string? id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var targetUserId = id ?? currentUserId;

            if (string.IsNullOrEmpty(targetUserId))
            {
                return RedirectToAction("SignIn", "Auth");
            }

            var profile = await _mediator.Send(new GetUserProfileQuery 
            { 
                UserId = targetUserId,
                CurrentUserId = currentUserId
            });
            
            if (profile == null)
            {
                 return NotFound();
            }

            return View(profile);
        }

        [HttpGet("verification")]
        public async Task<IActionResult> Verification()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("SignIn", "Auth");
            }

            var profile = await _mediator.Send(new GetUserProfileQuery { UserId = userId });
            if (profile == null) return NotFound();

            // We could fetch the VerificationRequest here but since we don't have a Query for it yet,
            // we'll leave it to the view to handle or simply pass the profile for now.
            // Actually, let's just use the profile's IsVerified status as the primary indicator.
            
            return View(profile);
        }

        [HttpPost("verification/submit")]
        public async Task<IActionResult> SubmitVerification(IFormFile verificationDoc)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("SignIn", "Auth");

            if (verificationDoc == null)
            {
                TempData["Error"] = "Please select a document (ID or University Card).";
                return RedirectToAction("Index");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(verificationDoc.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Invalid file format. Please upload a PNG or JPG image.";
                return RedirectToAction("Index");
            }

            var docUrl = await _fileService.UploadFileAsync(verificationDoc.OpenReadStream(), verificationDoc.FileName, "verifications");

            var command = new SubmitVerificationRequestCommand
            {
                UserId = userId,
                StudentIdUrl = docUrl,
                CarnetUrl = docUrl
            };

            var errors = await _mediator.Send(command);

            if (errors.Count > 0)
            {
                TempData["Error"] = string.Join(" ", errors);
            }
            else
            {
                TempData["Success"] = "Verification request submitted successfully.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost("verification/cancel")]
        public async Task<IActionResult> CancelVerification()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("SignIn", "Auth");

            var dbContext = HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
            var request = await dbContext.VerificationRequests
                .FirstOrDefaultAsync(v => v.UserId == userId && v.Status == VerificationRequestStatus.Pending);

            if (request != null)
            {
                dbContext.VerificationRequests.Remove(request);
                await dbContext.SaveChangesAsync(default);
                TempData["Success"] = "Verification request cancelled.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update(AskNLearn.Application.Features.Users.Commands.UpdateUserProfile.UpdateUserProfileCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("SignIn", "Auth");
            }

            command.Id = userId;

            if (!ModelState.IsValid)
            {
               
                return RedirectToAction("Index"); 
            }

            var errors = await _mediator.Send(command);

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                var profile = await _mediator.Send(new GetUserProfileQuery { UserId = userId });
                return View("Index", profile);
            }

            return RedirectToAction("Index");
        }

        [HttpPost("send-connection/{userId}")]
        public async Task<IActionResult> SendConnection(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null || currentUserId == userId) return BadRequest();

            var dbContext = HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
            
            var existing = await dbContext.Friendships.AnyAsync(f => 
                (f.RequesterId == currentUserId && f.AddresseeId == userId) || 
                (f.RequesterId == userId && f.AddresseeId == currentUserId));

            if (!existing)
            {
                var friendship = new Friendship
                {
                    RequesterId = currentUserId,
                    AddresseeId = userId,
                    Status = FriendshipStatus.Pending
                };
                dbContext.Friendships.Add(friendship);
                
                // Notify user
                var notification = new Notification
                {
                    UserId = userId,
                    Title = "New Connection Request",
                    Message = $"{User.Identity?.Name} wants to connect with you.",
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Notifications.Add(notification);
                
                await dbContext.SaveChangesAsync(default);
                TempData["Success"] = "Connection request sent!";
            }

            return RedirectToAction("Index", new { id = userId });
        }

        [HttpPost("accept-connection/{userId}")]
        public async Task<IActionResult> AcceptConnection(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var dbContext = HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();

            var friendship = await dbContext.Friendships.FirstOrDefaultAsync(f => 
                f.RequesterId == userId && f.AddresseeId == currentUserId);

            if (friendship != null)
            {
                friendship.Status = FriendshipStatus.Accepted;
                await dbContext.SaveChangesAsync(default);
                TempData["Success"] = "Connection accepted!";
            }

            return RedirectToAction("Index", new { id = userId });
        }

        [HttpPost("reject-connection/{userId}")]
        public async Task<IActionResult> RejectConnection(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var dbContext = HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();

            var friendship = await dbContext.Friendships.FirstOrDefaultAsync(f => 
                f.RequesterId == userId && f.AddresseeId == currentUserId);

            if (friendship != null)
            {
                dbContext.Friendships.Remove(friendship);
                await dbContext.SaveChangesAsync(default);
                TempData["Success"] = "Connection request ignored.";
            }

            return RedirectToAction("Index", new { id = userId });
        }

        [HttpPost("remove-connection/{userId}")]
        public async Task<IActionResult> RemoveConnection(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var dbContext = HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();

            var friendship = await dbContext.Friendships.FirstOrDefaultAsync(f => 
                (f.RequesterId == currentUserId && f.AddresseeId == userId) || 
                (f.RequesterId == userId && f.AddresseeId == currentUserId));

            if (friendship != null)
            {
                dbContext.Friendships.Remove(friendship);
                await dbContext.SaveChangesAsync(default);
                TempData["Success"] = "Connection removed.";
            }

            return RedirectToAction("Index", new { id = userId });
        }
        [AllowAnonymous]
        [HttpGet("hover-card/{id}")]
        public async Task<IActionResult> GetHoverCard(string id)
        {
            var profile = await _mediator.Send(new GetUserProfileQuery { UserId = id });
            if (profile == null) return NotFound();
            return PartialView("_UserHoverCard", profile);
        }

        [HttpGet("verification-status/{userId}")]
        public async Task<IActionResult> GetVerificationStatus(string userId)
        {
            var dbContext = HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
            var request = await dbContext.VerificationRequests
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.SubmittedAt)
                .Select(v => new 
                { 
                    v.Status, 
                    v.AdminNotes,
                    v.SubmittedAt,
                    v.ProcessedAt,
                    IsVerified = v.Status == VerificationRequestStatus.Approved
                })
                .FirstOrDefaultAsync();

            if (request == null) return NotFound();

            return Ok(request);
        }
    }
}
