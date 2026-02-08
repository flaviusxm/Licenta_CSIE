using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Models.Core;

namespace AskNLearn.Models.SocialFeed
{
    public enum CommunityRole
    {
        Member,     
        Moderator,  
        Admin       
    }

    [PrimaryKey(nameof(CommunityId), nameof(UserId))]
    [Table("CommunityMemberships")]
    public class CommunityMembership
    {
        public Guid CommunityId { get; set; }

        [ForeignKey(nameof(CommunityId))]
        public Community Community { get; set; } = null!;

        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        public CommunityRole Role { get; set; } = CommunityRole.Member;

        public bool IsMuted { get; set; } = false;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}