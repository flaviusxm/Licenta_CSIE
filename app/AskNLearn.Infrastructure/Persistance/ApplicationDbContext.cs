using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.Gamification;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.StudyGroup;
using AskNLearn.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AskNLearn.Infrastructure.Persistance
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
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
        public DbSet<PostView> PostViews { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<PostTag> PostTags { get; set; }

        // StudyGroup
        public DbSet<Channel> Channels { get; set; }
        public DbSet<ChannelCategory> ChannelCategories { get; set; }
        public DbSet<GroupInvite> GroupInvites { get; set; }
        public DbSet<GroupMembership> GroupMemberships { get; set; }
        public DbSet<GroupRole> GroupRoles { get; set; }
        public DbSet<StudyGroup> StudyGroups { get; set; }
        public DbSet<LearningResource> LearningResources { get; set; }
        public DbSet<Event> Events { get; set; }

        // Explicit interface implementations if needed, but here we just need public DbSets
        // that match the interface property names.
        DbSet<ApplicationUser> IApplicationDbContext.Users => Users;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ================================================================
            // REDENUMIRE TABELE IDENTITY - elimină prefixul AspNet
            // ================================================================
            
            // Tabela Users -> Users
            builder.Entity<ApplicationUser>().ToTable("Users");
            
            // Tabela Roles -> Roles
            builder.Entity<IdentityRole>().ToTable("Roles");
            
            // Tabela UserRoles -> UserRoles
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            
            // Tabela UserClaims -> UserClaims
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            
            // Tabela UserLogins -> UserLogins
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            
            // Tabela UserTokens -> UserTokens
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
            
            // Tabela RoleClaims -> RoleClaims
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");

            // Indexes for Moderation performance
            builder.Entity<Post>()
                .HasIndex(p => new { p.ModerationStatus, p.CreatedAt });

            builder.Entity<Message>()
                .HasIndex(m => new { m.ModerationStatus, m.CreatedAt });

            builder.Entity<Report>()
                .HasIndex(r => new { r.Status, r.CreatedAt });

            // Indexes for Verification performance
            builder.Entity<VerificationRequest>()
                .HasIndex(v => new { v.Status, v.SubmittedAt });
            builder.Entity<Post>()
                .HasIndex(p => new { p.ModerationStatus, p.CreatedAt });

            builder.Entity<Message>()
                .HasIndex(m => new { m.ModerationStatus, m.CreatedAt });

            builder.Entity<Report>()
                .HasIndex(r => new { r.Status, r.CreatedAt });

            // Indexes for Verification performance
            builder.Entity<VerificationRequest>()
                .HasIndex(v => new { v.Status, v.SubmittedAt });
            
            builder.Entity<VerificationRequest>()
                .HasIndex(v => v.UserId);

            // Friendship Configuration (Self-referencing relationship)
            builder.Entity<Friendship>(entity =>
            {
                entity.HasKey(f => new { f.RequesterId, f.AddresseeId });

                entity.HasOne(f => f.Requester)
                    .WithMany(u => u.FriendshipsRequested)
                    .HasForeignKey(f => f.RequesterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Addressee)
                    .WithMany(u => u.FriendshipsReceived)
                    .HasForeignKey(f => f.AddresseeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}