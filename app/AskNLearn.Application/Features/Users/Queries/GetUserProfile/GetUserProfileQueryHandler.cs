using MediatR;
using Microsoft.AspNetCore.Identity;
using AskNLearn.Domain.Entities.Core;
using AskNLearn.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Application.Features.Users.Queries.GetUserProfile
{
    public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationDbContext _context;

        public GetUserProfileQueryHandler(UserManager<ApplicationUser> userManager, IApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);

            if (user == null)
            {
                return null;
            }

            var postsCount = await _context.Posts.CountAsync(p => p.AuthorId == user.Id, cancellationToken);
            var answersCount = await _context.Messages.CountAsync(m => m.AuthorId == user.Id && m.PostId != null, cancellationToken);
            
            var communityCount = await _context.CommunityMemberships.CountAsync(cm => cm.UserId == user.Id, cancellationToken);
            var studyGroupCount = await _context.GroupMemberships.CountAsync(gm => gm.UserId == user.Id, cancellationToken);
            var groupsCount = communityCount + studyGroupCount;

            string? rankName = null;
            string? rankIconUrl = null;

            if (user.CurrentRankId != null)
            {
                var rank = await _context.UserRanks.FindAsync(new object[] { user.CurrentRankId.Value }, cancellationToken);
                if (rank != null)
                {
                    rankName = rank.Name;
                    rankIconUrl = rank.IconUrl;
                }
            }

            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName ?? "Anonymous",
                Email = user.Email ?? string.Empty,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl ?? "https://lh3.googleusercontent.com/aida-public/AB6AXuAtQnOLJpZ-UsH6E0M-Pwa3j5tbQuEa2K5UcyoFSPDLaeLZOCaR6pd7QYA3_usiM6RQUgtVwdWN3Ct6PabOSxI4CSvafcU5D9omVyYVtPrlSI_HkrJmfXLFU-I8_kRKAqqOJ_z-zPx5902KftyNultb0BHoXi6_r8SjUsVT1SqWu2nRUoLmlDZOVhsPe1ZEIEZ77oeWzV-f9qK9kaaGo4t_2GeUbe4MxMLioTEe0l4IlUMw0XebZAbn6gdCiulgIp6pwCnpCJPF",
                ReputationPoints = user.ReputationPoints,
                CreatedAt = user.CreatedAt,
                Level = user.Level,
                RankName = rankName,
                RankIconUrl = rankIconUrl,
                PostsCount = postsCount,
                AnswersCount = answersCount,
                GroupsCount = groupsCount
            };
        }
    }
}
