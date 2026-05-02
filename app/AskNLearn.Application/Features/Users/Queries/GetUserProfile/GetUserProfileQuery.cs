using MediatR;
using AskNLearn.Application.Common.Models;

namespace AskNLearn.Application.Features.Users.Queries.GetUserProfile
{
    public class GetUserProfileQuery : IRequest<UserProfileDto>
    {
        public string UserId { get; set; } = string.Empty;
        public string? CurrentUserId { get; set; }
    }

    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Occupation { get; set; }
        public string? Institution { get; set; }
        public string? Interests { get; set; }
        public int ReputationPoints { get; set; }
        public int ProfileCompletionPercentage { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public int Level { get; set; }
        public string? RankName { get; set; }
        public string? RankIconUrl { get; set; }
        public int PostsCount { get; set; }
        public int AnswersCount { get; set; }
        public int GroupsCount { get; set; }
        public bool IsVerified { get; set; }
        public string Role { get; set; } = "Member";
        public string? BannerUrl { get; set; }
        public string? SocialLinks { get; set; }
        public bool HasPendingVerification { get; set; }
        public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.None;
        public bool IsOwnProfile { get; set; }
        public bool EmailConfirmed { get; set; }
        public string VerificationStatus { get; set; } = "NotVerified";
        public string? AdminNotes { get; set; }
    }
}
