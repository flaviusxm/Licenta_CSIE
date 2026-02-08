using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Models.SocialFeed
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
        public Post Post { get; set; } = null!;

        public Guid TagId { get; set; }

        [ForeignKey(nameof(TagId))]
        public Tag Tag { get; set; } = null!;
    }
}