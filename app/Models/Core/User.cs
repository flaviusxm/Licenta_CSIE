using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AskNLearn.Models.Gamification;

namespace AskNLearn.Models.Core
{
    public enum Role{
        Member,
        Moderator,
        Admin,
    }
    public enum UserStatus {
    Offline,
    Idle,
    Online
    }

    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string? FullName { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        public string? AvatarUrl { get; set; }
        public string? BannerUrl { get; set; }

        public int ReputationPoints { get; set; } = 0;

        public Guid? CurrentRankId { get; set; }
        [ForeignKey(nameof(CurrentRankId))]
        public UserRank? CurrentRank { get; set; }

        public bool IsVerified { get; set; } = false;

        public Role Role { get; set; } = Role.Member;

        public UserStatus Status { get; set; } = UserStatus.Offline;
        public DateTime? LastActive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
