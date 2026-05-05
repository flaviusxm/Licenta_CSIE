using AskNLearn.Application.Features.Communities.Queries.GetCommunities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    [Route("hubs/communities/explore")]
    public class ExploreController : Controller
    {
        private readonly IMediator _mediator;

        public ExploreController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? searchTerm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var communities = await _mediator.Send(new GetCommunitiesQuery 
            { 
                SearchTerm = searchTerm,
                CurrentUserId = userId,
                Skip = 0,
                Take = 12
            });
            return View(communities);
        }

        [HttpGet("v1/batch")]
        public async Task<IActionResult> GetCommunities(int skip, string? searchTerm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var communities = await _mediator.Send(new GetCommunitiesQuery
            {
                SearchTerm = searchTerm,
                CurrentUserId = userId,
                Skip = skip,
                Take = 12
            });
            return PartialView("_CommunityCards", communities);
        }
    }
}
