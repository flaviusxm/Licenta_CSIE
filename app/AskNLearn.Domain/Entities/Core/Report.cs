using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AskNLearn.Domain.Entities.SocialFeed;

namespace AskNLearn.Domain.Entities.Core
{

    [Table("Reports")]
    public class Report
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string ReporterId { get; set; } = null!;
        
        [ForeignKey(nameof(ReporterId))]
        public ApplicationUser Reporter { get; set; } = null!;

        public Guid? ReportedPostId { get; set; }
        
        [ForeignKey(nameof(ReportedPostId))]
        public Post? ReportedPost { get; set; }

        public Guid? ReportedCommentId { get; set; }
        
        [ForeignKey(nameof(ReportedCommentId))]
        public Comment? ReportedComment { get; set; }

        public ReportReason Reason { get; set; }

        [Required]
        public string Description { get; set; } = null!;

        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}