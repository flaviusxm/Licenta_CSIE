using System.Collections.Generic;
using AskNLearn.Domain.Entities.Core;

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
        public int VoteCount { get; set; }
        public int UserVote { get; set; } // 1, -1, or 0
        public DateTime CreatedAt { get; set; }
        public List<CommentDto> Comments { get; set; } = [];
        public List<AttachmentDto> Attachments { get; set; } = [];
        public ModerationStatus ModerationStatus { get; set; }
        public string? ModerationReason { get; set; }
    }

    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid? ReplyToMessageId { get; set; }
        public string? AuthorId { get; set; }
        public string AuthorName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new();
        public ModerationStatus ModerationStatus { get; set; }
        public string? ModerationReason { get; set; }
    }

    public class AttachmentDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = null!;
        public string? FileType { get; set; }
    }
}
