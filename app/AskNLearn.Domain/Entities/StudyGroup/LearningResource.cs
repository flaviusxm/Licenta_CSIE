using AskNLearn.Domain.Entities.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities.StudyGroup
{
    [Table("LearningResources")]
    public class LearningResource
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid GroupId { get; set; }
        public string UploaderId { get; set; } = null!;
        
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = null!;
        
        public string Description { get; set; } = null!;
        
        [MaxLength(50)]
        public string ResourceType { get; set; } = null!;
        
        public string Url { get; set; } = null!;
        
        public int DownloadCount { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(GroupId))]
        public StudyGroup Group { get; set; } = null!;
        
        [ForeignKey(nameof(UploaderId))]
        public ApplicationUser Uploader { get; set; } = null!;
    }
}
