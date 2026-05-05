using MediatR;
using Microsoft.AspNetCore.Identity;
using AskNLearn.Domain.Entities.Core;
using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Application.Common.Models;
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
                return null!;
            }

            var postsCount = await _context.Posts.CountAsync(p => p.AuthorId == user.Id, cancellationToken);
            var answersCount = await _context.Comments.CountAsync(c => c.AuthorId == user.Id, cancellationToken);
            
            var communityCount = await _context.CommunityMemberships.CountAsync(cm => cm.UserId == user.Id, cancellationToken);
            var groupsCount = communityCount;

            var hasPendingVerification = await _context.VerificationRequests
                .AnyAsync(v => v.UserId == user.Id && v.Status == VerificationRequestStatus.Pending, cancellationToken);

            var completion = 0;
            if (!string.IsNullOrEmpty(user.FullName)) completion += 25;
            if (!string.IsNullOrEmpty(user.Bio)) completion += 25;
            if (!string.IsNullOrEmpty(user.AvatarUrl) && !user.AvatarUrl.Contains("dicebear")) completion += 25;
            if (user.IsVerified) completion += 25;

            // Connection Status Logic
            var connectionStatus = ConnectionStatus.None;
            if (!string.IsNullOrEmpty(request.CurrentUserId) && request.CurrentUserId != user.Id)
            {
                var friendship = await _context.Friendships.FirstOrDefaultAsync(f => 
                    (f.RequesterId == request.CurrentUserId && f.AddresseeId == user.Id) || 
                    (f.RequesterId == user.Id && f.AddresseeId == request.CurrentUserId), cancellationToken);

                if (friendship != null)
                {
                    if (friendship.Status == FriendshipStatus.Accepted)
                        connectionStatus = ConnectionStatus.Accepted;
                    else if (friendship.Status == FriendshipStatus.Pending)
                    {
                        connectionStatus = friendship.RequesterId == request.CurrentUserId 
                            ? ConnectionStatus.PendingSent 
                            : ConnectionStatus.PendingReceived;
                    }
                }
            }

            var userWithRank = await _context.Users
                .Include(u => u.CurrentRank)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (userWithRank == null) return null!;

            return new UserProfileDto
            {
                Id = userWithRank.Id,
                FullName = userWithRank.FullName ?? "Anonymous",
                Email = userWithRank.Email ?? string.Empty,
                Bio = userWithRank.Bio,
                AvatarUrl = userWithRank.AvatarUrl,
                ProfileCompletionPercentage = Math.Min(completion, 100),
                CreatedAt = userWithRank.CreatedAt,
                PostsCount = postsCount,
                AnswersCount = answersCount,
                IsVerified = userWithRank.IsVerified,
                Role = userWithRank.Role.ToString(),
                BannerUrl = userWithRank.BannerUrl,
                HasPendingVerification = hasPendingVerification,
                ConnectionStatus = connectionStatus,
                IsOwnProfile = request.CurrentUserId == userWithRank.Id,
                EmailConfirmed = userWithRank.EmailConfirmed,
                VerificationStatus = userWithRank.VerificationStatus.ToString(),
                ReputationPoints = userWithRank.ReputationPoints,
                Level = userWithRank.Level,
                RankName = userWithRank.CurrentRank?.Name,
                Interests = userWithRank.Interests,
                SocialLinks = userWithRank.SocialLinks,
                AdminNotes = await _context.VerificationRequests
                    .Where(v => v.UserId == userWithRank.Id)
                    .OrderByDescending(v => v.SubmittedAt)
                    .Select(v => v.AdminNotes)
                    .FirstOrDefaultAsync(cancellationToken)
            };
        }
    }
}
