using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Domain.Entities.SocialFeed
{
    [Table("Posts")]
    public class Post
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? CommunityId { get; set; }

        public string? AuthorId { get; set; }

        [ForeignKey(nameof(AuthorId))]
        public ApplicationUser? Author { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!; 

        public bool IsSolved { get; set; } = false;
        public bool IsLocked { get; set; } = false;
        public bool IsPinned { get; set; } = false;
        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;
        public string? ModerationReason { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<PostVote> Votes { get; set; } = new List<PostVote>();
        public ICollection<PostAttachment> Attachments { get; set; } = new List<PostAttachment>();
        public ICollection<PostView> UniqueViews { get; set; } = new List<PostView>();
        public ICollection<PostTag> Tags { get; set; } = new List<PostTag>();
    }
}
