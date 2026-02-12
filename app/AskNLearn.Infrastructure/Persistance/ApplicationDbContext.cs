using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.Gamification;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.StudyGroup;

namespace AskNLearn.Infrastructure.Persistance
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        // Core
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<StoredFile> StoredFiles { get; set; }
        public DbSet<VerificationRequest> VerificationRequests { get; set; }

        // Gamification
        public DbSet<UserRank> UserRanks { get; set; }

        // Messaging
        public DbSet<DirectConversation> DirectConversations { get; set; }
        public DbSet<DirectConversationParticipant> DirectConversationParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }

        // SocialFeed
        public DbSet<Community> Communities { get; set; }
        public DbSet<CommunityMembership> CommunityMemberships { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostAttachment> PostAttachments { get; set; }
        public DbSet<PostVote> PostVotes { get; set; }
        public DbSet<Tag> Tags { get; set; }

        // StudyGroup
        public DbSet<Channel> Channels { get; set; }
        public DbSet<ChannelCategory> ChannelCategories { get; set; }
        public DbSet<GroupInvite> GroupInvites { get; set; }
        public DbSet<GroupMembership> GroupMemberships { get; set; }
        public DbSet<GroupRole> GroupRoles { get; set; }
        public DbSet<StudyGroup> StudyGroups { get; set; }
    }
}
