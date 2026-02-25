using MediatR;
using System.Collections.Generic;

namespace AskNLearn.Application.Features.Communities.Queries.GetCommunities
{
    public class GetCommunitiesQuery : IRequest<List<CommunityDto>>
    {
        public string? SearchTerm { get; set; }
    }
}
