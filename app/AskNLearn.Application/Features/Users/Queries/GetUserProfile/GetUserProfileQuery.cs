using MediatR;

namespace AskNLearn.Application.Features.Users.Queries.GetUserProfile
{
    public class GetUserProfileQuery : IRequest<UserProfileDto>
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public int ReputationPoints { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public int Level { get; set; }
        public string? RankName { get; set; }
        public string? RankIconUrl { get; set; }
        public int PostsCount { get; set; }
        public int AnswersCount { get; set; }
        public int GroupsCount { get; set; }
    }
}
