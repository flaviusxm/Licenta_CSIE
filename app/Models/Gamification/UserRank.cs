using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Models.Gamification
{
    [Table("UserRanks")]
    public class UserRank
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        public int MinPoints { get; set; }

        public string? IconUrl { get; set; }
    }
}
