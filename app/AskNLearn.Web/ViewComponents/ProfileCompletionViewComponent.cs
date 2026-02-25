using AskNLearn.Application.Features.Users.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AskNLearn.Web.ViewComponents;

public class ProfileCompletionViewComponent : ViewComponent
{
    private readonly IMediator _mediator;

    public ProfileCompletionViewComponent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Content(string.Empty);
        }

        var profile = await _mediator.Send(new GetUserProfileQuery { UserId = userId });
        
        if (profile == null || profile.ProfileCompletionPercentage >= 100)
        {
            return Content(string.Empty);
        }

        return View(profile);
    }
}
