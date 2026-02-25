using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities.Core
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? ActorId { get; set; }

        [ForeignKey(nameof(ActorId))]
        public ApplicationUser? Actor { get; set; }

        [Required]
        [MaxLength(100)]
        public string ActionType { get; set; } = null!;

        [MaxLength(50)]
        public string? TargetEntity { get; set; } 

        public Guid? TargetId { get; set; }

        public string? Changes { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
