using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities.Core
{
    [Table("StoredFiles")]
    public class StoredFile
    {
        public enum ModuleContextType
        {
            Community,
            Group,
            Profile,
        }

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? UploaderId { get; set; }

        [ForeignKey(nameof(UploaderId))]
        public ApplicationUser? Uploader { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = null!;

        [Required]
        public string FilePath { get; set; } = null!;

        [MaxLength(100)]
        public string? FileType { get; set; } 
        
        public long? FileSize { get; set; } 

        [MaxLength(50)]
        public string? ModuleContext { get; set; } 

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
