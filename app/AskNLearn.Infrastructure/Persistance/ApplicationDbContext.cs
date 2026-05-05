using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.Gamification;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AskNLearn.Infrastructure.Persistance
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
    {
        // Core
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<StoredFile> StoredFiles { get; set; }
        public DbSet<VerificationRequest> VerificationRequests { get; set; }

        // Messaging
        public DbSet<DirectConversation> DirectConversations { get; set; }
        public DbSet<DirectConversationParticipant> DirectConversationParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }


        // SocialFeed
        public DbSet<Community> Communities { get; set; }
        public DbSet<CommunityMembership> CommunityMemberships { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostVote> PostVotes { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<PostAttachment> PostAttachments { get; set; }
        public DbSet<PostView> PostViews { get; set; }
        public DbSet<PostTag> PostTags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentAttachment> CommentAttachments { get; set; }

        // Gamification
        public DbSet<UserRank> UserRanks { get; set; }

        // Resources
        public DbSet<Resource> Resources { get; set; }

        DbSet<ApplicationUser> IApplicationDbContext.Users => Users;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ================================================================
            // IDENTITY CLEANUP - Omit unused fields from SQL
            // ================================================================
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                entity.Ignore(u => u.PhoneNumber);
                entity.Ignore(u => u.PhoneNumberConfirmed);
                entity.Ignore(u => u.TwoFactorEnabled);
                entity.Ignore(u => u.LockoutEnd);
                entity.Ignore(u => u.LockoutEnabled);
                entity.Ignore(u => u.AccessFailedCount);
                entity.Property(u => u.Status).HasConversion<string>();
            });

            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");

            // Indexes for Performance
            builder.Entity<Post>().HasIndex(p => new { p.ModerationStatus, p.CreatedAt });
            builder.Entity<Message>().HasIndex(m => new { m.ModerationStatus, m.CreatedAt });
            builder.Entity<Report>().HasIndex(r => new { r.Status, r.CreatedAt });
            builder.Entity<VerificationRequest>().HasIndex(v => new { v.Status, v.SubmittedAt });
            builder.Entity<VerificationRequest>().HasIndex(v => v.UserId);

            // Composite Keys
            builder.Entity<PostTag>().HasKey(pt => new { pt.PostId, pt.TagId });
            builder.Entity<MessageAttachment>().HasKey(ma => new { ma.MessageId, ma.FileId });

            builder.Entity<Report>(entity =>
            {
                entity.ToTable(tb => tb.HasCheckConstraint("CK_Reports_Target", 
                    "(ReportedPostId IS NOT NULL AND ReportedCommentId IS NULL) OR (ReportedPostId IS NULL AND ReportedCommentId IS NOT NULL)"));
            });

            // Friendship Configuration
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

            builder.Entity<Post>(entity =>
            {
                entity.HasOne(p => p.Author)
                    .WithMany()
                    .HasForeignKey(p => p.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Message>(entity =>
            {
                entity.HasOne(m => m.Author)
                    .WithMany()
                    .HasForeignKey(m => m.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Report>(entity =>
            {
                entity.HasOne(r => r.Reporter)
                    .WithMany()
                    .HasForeignKey(r => r.ReporterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}