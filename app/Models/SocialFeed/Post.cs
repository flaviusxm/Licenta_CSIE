using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AskNLearn.Models.Core;
using AskNLearn.Models.Messaging;
using AskNLearn.Models.StudyGroup; 

namespace AskNLearn.Models.SocialFeed
{
    [Table("Posts")]
    public class Post
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? CommunityId { get; set; } 

        public Guid? ChannelId { get; set; }

        [ForeignKey(nameof(ChannelId))]
        public Channel? Channel { get; set; }

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

        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Message> Comments { get; set; } = new List<Message>();

        public ICollection<PostAttachment> Attachments { get; set; } = new List<PostAttachment>();
        
        public ICollection<PostVote> Votes { get; set; } = new List<PostVote>();
    }
}
