using AskNLearn.Application.Features.Posts.Queries.GetPostsByCommunity;
using MediatR;
using System;

namespace AskNLearn.Application.Features.Posts.Queries.GetPostById
{
    public class GetPostByIdQuery : IRequest<PostDto?>
    {
        public Guid Id { get; set; }
    }
}
