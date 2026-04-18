using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.Messaging;

namespace AskNLearn.Domain.Entities.Core
{
    [Table("Reports")]
    public class Report
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public string ReporterId { get; set; } = null!;
        
        public Guid? ReportedPostId { get; set; }
        public Guid? ReportedMessageId { get; set; }
        
        public ReportReason Reason { get; set; }
        
        public string Description { get; set; } = null!;
        
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(ReporterId))]
        public ApplicationUser Reporter { get; set; } = null!;
        
        [ForeignKey(nameof(ReportedPostId))]
        public Post? ReportedPost { get; set; }
        
        [ForeignKey(nameof(ReportedMessageId))]
        public Message? ReportedMessage { get; set; }

    }
}