using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Domain.Entities.SocialFeed
{
    [Table("Tags")]
    public class Tag
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!; 

        public int UsageCount { get; set; } = 0; 
    }

    [PrimaryKey(nameof(PostId), nameof(TagId))]
    [Table("PostTags")]
    public class PostTag
    {
        public Guid PostId { get; set; }

        [ForeignKey(nameof(PostId))]
        public required Post Post { get; set; }

        public Guid TagId { get; set; }

        [ForeignKey(nameof(TagId))]
        public required Tag Tag { get; set; }
    }
}