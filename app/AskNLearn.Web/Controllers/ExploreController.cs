using AskNLearn.Application.Features.Communities.Queries.GetCommunities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AskNLearn.Web.Controllers
{
    public class ExploreController : Controller
    {
        private readonly IMediator _mediator;

        public ExploreController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index(string? searchTerm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var communities = await _mediator.Send(new GetCommunitiesQuery 
            { 
                SearchTerm = searchTerm,
                CurrentUserId = userId
            });
            return View(communities);
        }
    }
}
