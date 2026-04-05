using MediatR;
using System;

namespace AskNLearn.Application.Features.Posts.Queries.GetPostsByCommunity
{
    public class GetPostsCountByCommunityQuery : IRequest<int>
    {
        public Guid CommunityId { get; set; }
    }
}
