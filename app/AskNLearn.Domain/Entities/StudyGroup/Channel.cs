using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AskNLearn.Domain.Entities.Messaging;

namespace AskNLearn.Domain.Entities.StudyGroup
{
    public enum Type
    {
        Text,
        Voice
    }
    [Table("Channels")]
    public class Channel
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid GroupId { get; set; }

        [ForeignKey(nameof(GroupId))]
        public StudyGroup Group { get; set; } = null!;

        public Guid? CategoryId { get; set; }
  
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public Type Type { get; set; } = Type.Text;

        public bool IsPrivate { get; set; } = false;

        [MaxLength(255)]
        public string? Topic { get; set; }

        public int Position { get; set; } = 0;
        public ICollection<Message> Messages { get; set; } = [];
    }
}
