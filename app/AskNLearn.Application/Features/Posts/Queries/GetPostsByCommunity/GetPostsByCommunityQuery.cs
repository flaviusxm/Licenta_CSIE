using MediatR;
using System;
using System.Collections.Generic;

namespace AskNLearn.Application.Features.Posts.Queries.GetPostsByCommunity
{
    public class GetPostsByCommunityQuery : IRequest<List<PostDto>>
    {
        public Guid CommunityId { get; set; }
    }
}
