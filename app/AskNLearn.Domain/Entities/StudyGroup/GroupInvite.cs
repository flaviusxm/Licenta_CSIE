using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities.StudyGroup
{
    [Table("GroupInvites")]
    public class GroupInvite
    {
        [Key] public Guid Id { get; set; }
        [Required][MaxLength(20)] public string Code { get; set; } 
        public Guid GroupId { get; set; } 
        public string CreatorId { get; set; } 
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; } 
        public int MaxUses { get; set; } = 0; 
        public int CurrentUses { get; set; } = 0;
    }
}
