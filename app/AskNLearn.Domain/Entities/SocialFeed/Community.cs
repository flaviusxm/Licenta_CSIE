using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities.SocialFeed
{
    [Table("Communities")]
    public class Community
    {
        [Key] public Guid Id { get; set; }
        [Required][MaxLength(100)] public string Name { get; set; }
        [Required][MaxLength(100)] public string Slug { get; set; } 
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? CreatorId { get; set; } 
        public DateTime CreatedAt { get; set; }
        public ICollection<Post> Posts { get; set; }
    }
}
