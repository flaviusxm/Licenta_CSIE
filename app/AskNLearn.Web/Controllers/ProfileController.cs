using AskNLearn.Application.Features.Users.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Application.Features.Users.Commands.SubmitVerificationRequest;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    [Route("identity/profiles")]
    public class ProfileController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IFileService _fileService;

        public ProfileController(IMediator mediator, IFileService fileService)
        {
            _mediator = mediator;
            _fileService = fileService;
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
        public async Task<IActionResult> SubmitVerification(IFormFile studentId, IFormFile carnet)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("SignIn", "Auth");

            if (studentId == null || carnet == null)
            {
                TempData["Error"] = "Both Student ID and Carnet are required.";
                return RedirectToAction("Index");
            }

            var studentIdUrl = await _fileService.UploadFileAsync(studentId.OpenReadStream(), studentId.FileName, "verifications");
            var carnetUrl = await _fileService.UploadFileAsync(carnet.OpenReadStream(), carnet.FileName, "verifications");

            var command = new SubmitVerificationRequestCommand
            {
                UserId = userId,
                StudentIdUrl = studentIdUrl,
                CarnetUrl = carnetUrl
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
                .FirstOrDefaultAsync(v => v.UserId == userId && v.Status == Status.Pending);

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
                    IsVerified = v.Status == Status.Approved
                })
                .FirstOrDefaultAsync();

            if (request == null) return NotFound();

            return Ok(request);
        }
    }
}
