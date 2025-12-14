using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Models.Core

{
   public enum VerificationStatus{
    Pending,
    Approved,
    Rejected
   }
    [Table("VerificationRequests")]
    public class VerificationRequest
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public string StudentIdUrl { get; set; }

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending; 

        public string? AdminNotes { get; set; }

        public string? ProcessedBy { get; set; }
        
        [ForeignKey(nameof(ProcessedBy))]
        public ApplicationUser? ProcessedByUser { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
