using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities.Core
{
    [Table("Resources")]
    public class Resource
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? UploaderId { get; set; }
        
        [ForeignKey(nameof(UploaderId))]
        public ApplicationUser? Uploader { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public string FileUrl { get; set; } = null!;

        public string? FileType { get; set; }

        public long? FileSize { get; set; }

        public int DownloadCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
