using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.Gamification;
using AskNLearn.Domain.Entities.Messaging;
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
        DbSet<Notification> Notifications { get; }
        DbSet<Message> Messages { get; }
        DbSet<UserRank> UserRanks { get; }
        DbSet<PostVote> PostVotes { get; }
        DbSet<PostAttachment> PostAttachments { get; }
        DbSet<PostView> PostViews { get; }
        DbSet<StoredFile> StoredFiles { get; }
        DbSet<MessageAttachment> MessageAttachments { get; }
        DbSet<MessageReaction> MessageReactions { get; }
        DbSet<VerificationRequest> VerificationRequests { get; }
        DbSet<Friendship> Friendships { get; }
        DbSet<DirectConversationParticipant> DirectConversationParticipants { get; }
        
        DbSet<PostTag> PostTags { get; }
        DbSet<Report> Reports { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
