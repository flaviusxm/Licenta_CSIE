using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Models.Core;
using AskNLearn.Models.Messaging;
using AskNLearn.Models.SocialFeed;
using AskNLearn.Models.StudyGroup;
using AskNLearn.Models.Gamification;

namespace AskNLearn.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<VerificationRequest> VerificationRequests { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<StoredFile> StoredFiles { get; set; }
        public DbSet<StudyGroup> StudyGroups { get; set; }
        public DbSet<GroupRole> GroupRoles { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }
        public DbSet<DirectConversation> DirectConversations { get; set; }
        public DbSet<DirectConversationParticipant> DirectConversationParticipants { get; set; }
        
        public DbSet<UserRank> UserRanks { get; set; }
        
        public DbSet<Community> Communities { get; set; }
        
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostAttachment> PostAttachments { get; set; }
        public DbSet<PostVote> PostVotes { get; set; }
        
        public DbSet<GroupMembership> GroupMemberships { get; set; }

        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<GroupMembership>()
                .HasKey(gm => new { gm.GroupId, gm.UserId });

            builder.Entity<GroupMembership>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade); 

            builder.Entity<StudyGroup>()
                .HasMany(g => g.Channels)
                .WithOne(c => c.Group)
                .HasForeignKey(c => c.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Community>()
                .HasMany(c => c.Posts)
                .WithOne()
                .HasForeignKey(p => p.CommunityId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Post>()
                .HasMany(p => p.Comments)
                .WithOne(m => m.Post)
                .HasForeignKey(m => m.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<Post>()
                .HasMany(p => p.Attachments)
                .WithOne(a => a.Post)
                .HasForeignKey(a => a.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Message>()
                .HasOne(m => m.Author)
                .WithMany()
                .HasForeignKey(m => m.AuthorId)
                .OnDelete(DeleteBehavior.Restrict); 

            builder.Entity<Message>()
                .HasOne(m => m.ReplyToMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.Restrict); 

            builder.Entity<DirectConversationParticipant>()
                .HasKey(dcp => new { dcp.ConversationId, dcp.UserId });

            builder.Entity<DirectConversationParticipant>()
                .HasOne(dcp => dcp.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(dcp => dcp.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

          
            builder.Entity<Friendship>()
                .HasKey(f => new { f.RequesterId, f.AddresseeId });

            builder.Entity<Friendship>()
                .HasOne(f => f.Requester)
                .WithMany()
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.Restrict); 

            builder.Entity<Friendship>()
                .HasOne(f => f.Addressee)
                .WithMany()
                .HasForeignKey(f => f.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);
 
    
builder.Entity<PostVote>()
    .HasKey(pv => new { pv.PostId, pv.UserId }); 

builder.Entity<PostVote>()
    .HasOne(pv => pv.Post)
    .WithMany(p => p.Votes)     
    .HasForeignKey(pv => pv.PostId)
    .OnDelete(DeleteBehavior.Cascade); 
    }
}

}