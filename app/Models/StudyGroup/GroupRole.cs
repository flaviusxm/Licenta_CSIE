using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Models.StudyGroup
{
    [Table("GroupRoles")]
    public class GroupRole
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid GroupId { get; set; }

        [ForeignKey(nameof(GroupId))]
        public StudyGroup Group { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        [MaxLength(10)]
        public string? Color { get; set; } 

        public int Priority { get; set; } = 0;

        [Required]
        public string Permissions { get; set; }
    }
}
