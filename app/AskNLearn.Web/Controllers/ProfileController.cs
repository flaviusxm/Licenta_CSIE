using AskNLearn.Application.Features.Users.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IMediator _mediator;

        public ProfileController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("SignIn", "Auth");
            }

            var profile = await _mediator.Send(new GetUserProfileQuery { UserId = userId });
            
            if (profile == null)
            {
                 return NotFound();
            }

            return View(profile);
        }
        [HttpPost]
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
    }
}
