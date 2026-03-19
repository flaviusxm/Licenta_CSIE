using AskNLearn.Domain.Entities.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities.SocialFeed
{
    [Table("Events")]
    public class Event
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid CommunityId { get; set; }
        public string OrganizerId { get; set; } = null!;
        
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = null!;
        
        public string Description { get; set; } = null!;
        
        [MaxLength(255)]
        public string Location { get; set; } = null!;
        
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public int MaxAttendees { get; set; }
        public int CurrentAttendees { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(CommunityId))]
        public Community Community { get; set; } = null!;
        
        [ForeignKey(nameof(OrganizerId))]
        public ApplicationUser Organizer { get; set; } = null!;
    }
}
