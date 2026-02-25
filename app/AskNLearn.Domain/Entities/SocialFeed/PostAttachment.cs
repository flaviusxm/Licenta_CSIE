using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Domain.Entities.SocialFeed
{
    [Table("PostAttachments")]
    public class PostAttachment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PostId { get; set; }
        [ForeignKey(nameof(PostId))]
        public Post Post { get; set; } = null!;

        [Required]
        public string Url { get; set; } = null!;

        public string? FileType { get; set; } 
}
}