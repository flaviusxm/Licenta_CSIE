using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities.Core
{
    public enum ReportReason
    {
        Spam,
        Harassment,
        InappropriateContent,
        Misinformation,
        Other
    }

    public enum ReportStatus
    {
        Pending,
        Reviewed,
        Dismissed,
        ActionTaken
    }

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
        public Guid? ReportedMessageId { get; set; }
        public string? ReportedUserId { get; set; }

        [Required]
        public ReportReason Reason { get; set; }

        public string? Description { get; set; }

        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public string? ResolvedById { get; set; }

        [ForeignKey(nameof(ResolvedById))]
        public ApplicationUser? ResolvedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}