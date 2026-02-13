using MediatR;
using Microsoft.AspNetCore.Identity;
using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Application.Features.Users.Queries.GetUserProfile
{
    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public GetUserProfileQueryHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);

            if (user == null)
            {
                return null;
            }

            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl ?? "https://lh3.googleusercontent.com/aida-public/AB6AXuAtQnOLJpZ-UsH6E0M-Pwa3j5tbQuEa2K5UcyoFSPDLaeLZOCaR6pd7QYA3_usiM6RQUgtVwdWN3Ct6PabOSxI4CSvafcU5D9omVyYVtPrlSI_HkrJmfXLFU-I8_kRKAqqOJ_z-zPx5902KftyNultb0BHoXi6_r8SjUsVT1SqWu2nRUoLmlDZOVhsPe1ZEIEZ77oeWzV-f9qK9kaaGo4t_2GeUbe4MxMLioTEe0l4IlUMw0XebZAbn6gdCiulgIp6pwCnpCJPF", // Default avatar
                ReputationPoints = user.ReputationPoints,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
