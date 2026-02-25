using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.Gamification;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Domain.Entities.StudyGroup;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Community> Communities { get; }
        DbSet<Post> Posts { get; }
        DbSet<ApplicationUser> Users { get; }
        
        DbSet<CommunityMembership> CommunityMemberships { get; }
        DbSet<Message> Messages { get; }
        DbSet<UserRank> UserRanks { get; }
        DbSet<GroupMembership> GroupMemberships { get; }
        DbSet<StudyGroup> StudyGroups { get; }
        DbSet<PostVote> PostVotes { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
