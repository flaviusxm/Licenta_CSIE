using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities.SocialFeed
{
    [Table("CommentAttachments")]
    public class CommentAttachment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CommentId { get; set; }
        
        [ForeignKey(nameof(CommentId))]
        public Comment Comment { get; set; } = null!;

        [Required]
        public string Url { get; set; } = null!;

        public string? FileType { get; set; }
    }
}
