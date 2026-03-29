using MediatR;
using System.Collections.Generic;

namespace AskNLearn.Application.Features.Communities.Queries.GetCommunities
{
    public class GetCommunitiesQuery : IRequest<List<CommunityDto>>
    {
        public string? SearchTerm { get; set; }
        public string? CurrentUserId { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 12;
    }
}
