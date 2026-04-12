using MediatR;
using System.Collections.Generic;

namespace AskNLearn.Application.Features.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommand : IRequest<List<string>>
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Occupation { get; set; }
        public string? Institution { get; set; }
        public string? Interests { get; set; }
        public string? BannerUrl { get; set; }
        public string? SocialLinks { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? AvatarFile { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? BannerFile { get; set; }
    }
}
