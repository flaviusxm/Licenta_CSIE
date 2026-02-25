using AskNLearn.Application.Features.Communities.Queries.GetCommunities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
            var communities = await _mediator.Send(new GetCommunitiesQuery { SearchTerm = searchTerm });
            return View(communities);
        }
    }
}
