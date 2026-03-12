using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.Gamification;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Domain.Entities.StudyGroup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AskNLearn.Infrastructure.Persistance
{
    public static class DatabaseInitializer
    {
        private static readonly Random _random = new Random();

        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            await SeedRanksAsync(context);
            await SeedTagsAsync(context);
            await SeedUsersAsync(userManager);
            var users = await context.Users.ToListAsync();
            await SeedStoredFilesAsync(context);
            await SeedVerificationRequestsAsync(context, users);
            await SeedFriendshipsAsync(context, users);
            await SeedCommunitiesAsync(context, users);
            await SeedStudyGroupsAsync(context, users);
            var groups = await context.StudyGroups.ToListAsync();
            await SeedGroupAddonsAsync(context, groups, users);
            await SeedChannelsAsync(context, groups);
            var communities = await context.Communities.ToListAsync();
            var channels = await context.Channels.ToListAsync();
            await SeedPostsAsync(context, communities, channels, users);
            var posts = await context.Posts.ToListAsync();
            await SeedMessagesAsync(context, posts, channels, users);
            var messages = await context.Messages.ToListAsync();
            await SeedEngagementAsync(context, posts, messages, users);
            await SeedDirectMessagingAsync(context, users);
            await SeedAuditAndReportsAsync(context, posts, messages, users);
        }

        private static async Task SeedRanksAsync(ApplicationDbContext context)
        {
            if (await context.UserRanks.AnyAsync()) return;
            context.UserRanks.AddRange(
                new UserRank { Id = Guid.NewGuid(), Name = "Novice", MinPoints = 0, IconUrl = "/icons/ranks/novice.png" },
                new UserRank { Id = Guid.NewGuid(), Name = "Scholar", MinPoints = 500, IconUrl = "/icons/ranks/scholar.png" },
                new UserRank { Id = Guid.NewGuid(), Name = "Expert", MinPoints = 1200, IconUrl = "/icons/ranks/expert.png" },
                new UserRank { Id = Guid.NewGuid(), Name = "Guru", MinPoints = 3000, IconUrl = "/icons/ranks/guru.png" }
            );
            await context.SaveChangesAsync();
        }

        private static async Task SeedTagsAsync(ApplicationDbContext context)
        {
            if (await context.Tags.AnyAsync()) return;
            var tagNames = new[] { "CSharp", "Database", "AI", "Algorithms", "React", "Calculus", "Microeconomics", "BigData" };
            foreach (var name in tagNames)
            {
                context.Tags.Add(new Tag { Id = Guid.NewGuid(), Name = name, UsageCount = _random.Next(5, 50) });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
        {
            var testUsers = new List<(string Email, string Name, Role Role)>
            {
                ("admin@asknlearn.com", "System Admin", Role.Admin),
                ("prof.andrei@asknlearn.com", "Prof. Andrei Popescu", Role.Moderator),
                ("prof.elena@asknlearn.com", "Prof. Elena Ionescu", Role.Moderator),
                ("flavius@asknlearn.com", "Flavius Mihai", Role.Member),
                ("maria@asknlearn.com", "Maria Dumitrescu", Role.Member),
                ("george@asknlearn.com", "George Vasile", Role.Member),
                ("ana@asknlearn.com", "Ana Maria", Role.Member),
                ("stefan@asknlearn.com", "Stefan Radu", Role.Member),
                ("cristina@asknlearn.com", "Cristina Stan", Role.Member),
                ("alex@asknlearn.com", "Alexandru Marin", Role.Member)
            };

            foreach (var u in testUsers)
            {
                if (await userManager.FindByEmailAsync(u.Email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = u.Email,
                        Email = u.Email,
                        FullName = u.Name,
                        EmailConfirmed = true,
                        Role = u.Role,
                        IsVerified = u.Role != Role.Member,
                        ReputationPoints = _random.Next(0, 4000),
                        Bio = $"Bio for {u.Name}. Passionate about learning!",
                        Institution = u.Role == Role.Moderator ? "ASE Bucuresti" : "CSIE ASE",
                        Occupation = u.Role == Role.Moderator ? "Professor" : "Student",
                        CreatedAt = DateTime.UtcNow.AddMonths(-_random.Next(1, 12))
                    };
                    await userManager.CreateAsync(user, "TestPassword123!");
                }
            }
        }

        private static async Task SeedStoredFilesAsync(ApplicationDbContext context)
        {
            if (await context.StoredFiles.AnyAsync()) return;
            for (int i = 1; i <= 5; i++)
            {
                context.StoredFiles.Add(new StoredFile
                {
                    Id = Guid.NewGuid(),
                    FileName = $"resource_{i}.pdf",
                    StoragePath = $"/uploads/files/resource_{i}.pdf",
                    FileType = "application/pdf",
                    FileSize = _random.Next(10240, 1024000),
                    UploadedAt = DateTime.UtcNow.AddDays(-i)
                });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedVerificationRequestsAsync(ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.VerificationRequests.AnyAsync()) return;
            var students = users.Where(u => u.Role == Role.Member).Take(3).ToList();
            var admins = users.Where(u => u.Role == Role.Admin).ToList();

            foreach (var s in students)
            {
                context.VerificationRequests.Add(new VerificationRequest
                {
                    Id = Guid.NewGuid(),
                    UserId = s.Id,
                    StudentIdUrl = $"/uploads/id_{s.UserName}.jpg",
                    CarnetUrl = $"/uploads/carnet_{s.UserName}.jpg",
                    Status = _random.Next(3) == 0 ? Status.Pending : Status.Approved,
                    SubmittedAt = DateTime.UtcNow.AddDays(-10),
                    ProcessedBy = admins.FirstOrDefault()?.Id,
                    ProcessedAt = DateTime.UtcNow.AddDays(-5)
                });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedFriendshipsAsync(ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.Friendships.AnyAsync()) return;
            for (int i = 0; i < 10; i++)
            {
                var r1 = users[_random.Next(users.Count)];
                var r2 = users[_random.Next(users.Count)];
                if (r1.Id == r2.Id) continue;
                if (await context.Friendships.AnyAsync(f => (f.RequesterId == r1.Id && f.AddresseeId == r2.Id) || (f.RequesterId == r2.Id && f.AddresseeId == r1.Id))) continue;

                context.Friendships.Add(new Friendship
                {
                    RequesterId = r1.Id,
                    AddresseeId = r2.Id,
                    Status = (FriendshipStatus)_random.Next(3),
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedCommunitiesAsync(ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.Communities.AnyAsync()) return;
            var admin = users.First(u => u.Role == Role.Admin);
            var names = new[] { "Informatica Economica", "Cibernetica", "Statistica", "Contabilitate", "Marketing" };
            var communities = names.Select(name => new Community
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = name.ToLower().Replace(" ", "-"),
                Description = $"Grup oficial pentru facultatea de {name}.",
                CreatorId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            }).ToList();

            context.Communities.AddRange(communities);
            await context.SaveChangesAsync();

            // Seed memberships
            foreach (var c in communities)
            {
                foreach (var u in users.OrderBy(x => _random.Next()).Take(7))
                {
                    context.CommunityMemberships.Add(new CommunityMembership { CommunityId = c.Id, UserId = u.Id, JoinedAt = DateTime.UtcNow });
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedStudyGroupsAsync(ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.StudyGroups.AnyAsync()) return;
            var profs = users.Where(u => u.Role == Role.Moderator).ToList();
            var names = new[] { "Pregatire Licenta ASE", "C++ Advanced", "Data Science Hub", "Modelling Economics" };
            
            foreach (var name in names)
            {
                var owner = profs[_random.Next(profs.Count)];
                var group = new AskNLearn.Domain.Entities.StudyGroup.StudyGroup
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = $"Grup de studiu privat pentru {name}.",
                    OwnerId = owner.Id,
                    IsPublic = _random.Next(2) == 0,
                    InviteCode = "INV" + _random.Next(100, 999),
                    SubjectArea = "Economics/CS",
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                };
                context.StudyGroups.Add(group);
                context.GroupMemberships.Add(new GroupMembership { GroupId = group.Id, UserId = owner.Id, JoinedAt = DateTime.UtcNow });
                foreach (var u in users.Where(u => u.Id != owner.Id).OrderBy(x => _random.Next()).Take(4))
                {
                    context.GroupMemberships.Add(new GroupMembership { GroupId = group.Id, UserId = u.Id, JoinedAt = DateTime.UtcNow });
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedGroupAddonsAsync(ApplicationDbContext context, List<AskNLearn.Domain.Entities.StudyGroup.StudyGroup> groups, List<ApplicationUser> users)
        {
            if (await context.GroupRoles.AnyAsync()) return;
            foreach (var g in groups)
            {
                context.GroupRoles.Add(new GroupRole { GroupId = g.Id, Name = "Moderator", Color = "#FF0000", Permissions = "ALL", Priority = 10 });
                context.GroupRoles.Add(new GroupRole { GroupId = g.Id, Name = "Helper", Color = "#00FF00", Permissions = "KICK", Priority = 5 });
                
                context.ChannelCategories.Add(new ChannelCategory { GroupId = g.Id, Name = "TEXT CHANNELS", Position = 0 });
                context.ChannelCategories.Add(new ChannelCategory { GroupId = g.Id, Name = "VOICE CHANNELS", Position = 1 });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedChannelsAsync(ApplicationDbContext context, List<AskNLearn.Domain.Entities.StudyGroup.StudyGroup> groups)
        {
            if (await context.Channels.AnyAsync()) return;
            foreach (var g in groups)
            {
                context.Channels.Add(new Channel { Id = Guid.NewGuid(), StudyGroupId = g.Id, Name = "general", Type = ChannelType.Text, Topic = "General discussion" });
                context.Channels.Add(new Channel { Id = Guid.NewGuid(), StudyGroupId = g.Id, Name = "resurse", Type = ChannelType.Text, Topic = "Resource sharing" });
                context.Channels.Add(new Channel { Id = Guid.NewGuid(), StudyGroupId = g.Id, Name = "Lounge", Type = ChannelType.Voice });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedPostsAsync(ApplicationDbContext context, List<Community> communities, List<Channel> channels, List<ApplicationUser> users)
        {
            if (await context.Posts.AnyAsync()) return;
            foreach (var c in communities)
            {
                for (int i = 0; i < 3; i++)
                {
                    var author = users[_random.Next(users.Count)];
                    context.Posts.Add(new Post
                    {
                        Id = Guid.NewGuid(),
                        CommunityId = c.Id,
                        AuthorId = author.Id,
                        Title = $"Intrebare {c.Name} #{i}",
                        Content = $"Imi poate explica cineva conceptul X din cursul de {c.Name}?",
                        ViewCount = _random.Next(100, 1000),
                        CreatedAt = DateTime.UtcNow.AddDays(-i)
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedMessagesAsync(ApplicationDbContext context, List<Post> posts, List<Channel> channels, List<ApplicationUser> users)
        {
            if (await context.Messages.AnyAsync()) return;
            // Post Comments
            foreach (var p in posts)
            {
                for (int i = 0; i < 2; i++)
                {
                    context.Messages.Add(new Message
                    {
                        Id = Guid.NewGuid(),
                        PostId = p.Id,
                        AuthorId = users[_random.Next(users.Count)].Id,
                        Content = $"Seeded comment {i} for post {p.Title}",
                        CreatedAt = DateTime.UtcNow.AddHours(-i)
                    });
                }
            }
            foreach (var ch in channels.Where(x => x.Type == ChannelType.Text))
            {
                for (int i = 0; i < 5; i++)
                {
                    context.Messages.Add(new Message
                    {
                        Id = Guid.NewGuid(),
                        ChannelId = ch.Id,
                        AuthorId = users[_random.Next(users.Count)].Id,
                        Content = $"Hello in {ch.Name}! Message #{i}",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-i * 10)
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedEngagementAsync(ApplicationDbContext context, List<Post> posts, List<Message> messages, List<ApplicationUser> users)
        {
            if (await context.PostVotes.AnyAsync()) return;
            foreach (var p in posts)
            {
                foreach (var u in users.OrderBy(x => _random.Next()).Take(3))
                {
                    context.PostVotes.Add(new PostVote { PostId = p.Id, UserId = u.Id, Type = _random.Next(2) == 0 ? 1 : -1 });
                    context.PostViews.Add(new PostView { PostId = p.Id, UserId = u.Id, ViewedAt = DateTime.UtcNow });
                }
            }
            foreach (var m in messages.OrderBy(x => _random.Next()).Take(50))
            {
                context.MessageReactions.Add(new MessageReaction { MessageId = m.Id, UserId = users[_random.Next(users.Count)].Id, ReactionEmoji = "👍" });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedDirectMessagingAsync(ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.DirectConversations.AnyAsync()) return;
            for (int i = 0; i < 3; i++)
            {
                var u1 = users[i];
                var u2 = users[i+1];
                var conv = new DirectConversation { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
                context.DirectConversations.Add(conv);
                context.DirectConversationParticipants.Add(new DirectConversationParticipant { ConversationId = conv.Id, UserId = u1.Id });
                context.DirectConversationParticipants.Add(new DirectConversationParticipant { ConversationId = conv.Id, UserId = u2.Id });
                
                context.Messages.Add(new Message { Id = Guid.NewGuid(), ConversationId = conv.Id, AuthorId = u1.Id, Content = "Buna! Seeded DM.", CreatedAt = DateTime.UtcNow });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedAuditAndReportsAsync(ApplicationDbContext context, List<Post> posts, List<Message> messages, List<ApplicationUser> users)
        {
            if (await context.AuditLogs.AnyAsync()) return;
            foreach (var u in users.Take(5))
            {
                context.AuditLogs.Add(new AuditLog { ActorId = u.Id, ActionType = "LOGIN", TargetEntity = "Core", CreatedAt = DateTime.UtcNow });
                context.Notifications.Add(new Notification { UserId = u.Id, Title = "Welcome!", Message = "Your account has been seeded.", IsRead = false });
            }
            if (posts.Any())
            {
                context.Reports.Add(new Report { ReporterId = users.Last().Id, ReportedPostId = posts.First().Id, Reason = ReportReason.Spam, Description = "Automated seed report", Status = ReportStatus.Pending });
            }
            await context.SaveChangesAsync();
        }
    }
}
