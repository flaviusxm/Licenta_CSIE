using MediatR;
using System;
using System.Collections.Generic;
using AskNLearn.Application.Features.Posts.Queries.GetPostsByCommunity;

namespace AskNLearn.Application.Features.Posts.Queries.GetPostComments
{
    public class GetPostCommentsQuery : IRequest<PostCommentsResult>
    {
        public Guid PostId { get; set; }
        public Guid CommunityId { get; set; }
        public string? CurrentUserId { get; set; }
    }

    public class PostCommentsResult
    {
        public Guid PostId { get; set; }
        public Guid CommunityId { get; set; }
        public string? AuthorId { get; set; }
        public bool IsSolved { get; set; }
        public List<CommentDto> Comments { get; set; } = new();
    }
}
