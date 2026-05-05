using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Domain.Entities.SocialFeed
{
    [Table("Comments")]
    public class Comment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PostId { get; set; }
        [ForeignKey(nameof(PostId))]
        public Post Post { get; set; } = null!;

        public string? AuthorId { get; set; }
        [ForeignKey(nameof(AuthorId))]
        public ApplicationUser? Author { get; set; }

        public string Content { get; set; } = null!;

        public Guid? ReplyToCommentId { get; set; }
        [ForeignKey(nameof(ReplyToCommentId))]
        public Comment? ReplyToComment { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

        public bool IsEdited { get; set; } = false;

        public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;
        public string? ModerationReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<CommentAttachment> Attachments { get; set; } = new List<CommentAttachment>();
            }
}
