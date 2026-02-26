using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AskNLearn.Domain.Entities.Gamification;

namespace AskNLearn.Domain.Entities.Core
{
    public enum Role{
        Member,
        Moderator,
        Admin,
    }
    public enum UserStatus{
        Online,
        Offline,
        Away,
        DoNotDisturb
    }
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string? FullName { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        public string? AvatarUrl { get; set; }
        public string? BannerUrl { get; set; }

        public string? Occupation { get; set; } 
        public string? Institution { get; set; }
        public string? Interests { get; set; }

        public string? SocialLinks { get; set; } 

        public int ReputationPoints { get; set; } = 0;
        public int Level => ReputationPoints / 100; 

        public Guid? CurrentRankId { get; set; }
        [ForeignKey(nameof(CurrentRankId))]
        public UserRank? CurrentRank { get; set; }

        public bool IsVerified { get; set; } = false;

        public Role Role { get; set; } = Role.Member; 

        public string Status { get; set; } = "Offline";
        public DateTime? LastActive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
