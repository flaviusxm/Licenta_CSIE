using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities.StudyGroup
{
    [Table("ChannelCategories")]
    public class ChannelCategory
    {
        [Key] public Guid Id { get; set; }
        public Guid GroupId { get; set; } 
        [Required][MaxLength(50)] public string Name { get; set; }
        public int Position { get; set; } = 0;
        public ICollection<Channel> Channels { get; set; }
    }
}
