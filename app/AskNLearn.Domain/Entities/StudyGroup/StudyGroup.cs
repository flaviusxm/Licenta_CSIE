using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Domain.Entities.StudyGroup
{
    [Table("StudyGroups")]
    public class StudyGroup
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
        
        public string? Description { get; set; }

        public string? OwnerId { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public ApplicationUser? Owner { get; set; }

        public string? IconUrl { get; set; }

        [MaxLength(20)]
        public string? InviteCode { get; set; } 

        public bool IsPublic { get; set; } = false;

        [MaxLength(50)]
        public string? SubjectArea { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Channel> Channels { get; set; } = [];
        public ICollection<GroupMembership> Members { get; set; } = [];
    }
}
