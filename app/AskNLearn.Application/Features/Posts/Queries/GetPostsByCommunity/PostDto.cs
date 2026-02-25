using System;

namespace AskNLearn.Application.Features.Posts.Queries.GetPostsByCommunity
{
    public class PostDto
    {
        public Guid Id { get; set; }
        public Guid? CommunityId { get; set; }
        public string? AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsSolved { get; set; }
        public bool IsLocked { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
