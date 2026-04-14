using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.Gamification;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.StudyGroup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.SqlClient;

namespace AskNLearn.Infrastructure.Persistance
{
    public static class LoadTestDatabaseSeeder
    {
        public const string DefaultPassword = "Test@1234!";

        private static readonly Random _globalRng = new();
        private static readonly ThreadLocal<Random> _tlsRng = new(() => new Random(_globalRng.Next()));
        private static Random Rng => _tlsRng.Value!;

        public static async Task SeedAsync(ApplicationDbContext ctx, UserManager<ApplicationUser> userManager, bool force = false)
        {
            ctx.Database.SetCommandTimeout(600);
            Console.WriteLine("[Seeder] Starting comprehensive seed...");

            if (force) await ClearAllDataAsync(ctx);

            // 1. Ranks
            var ranks = await SeedRanksAsync(ctx);

            // 1.5 Roles
            var roles = await SeedIdentityRolesAsync(ctx);

            // 2. Users (with varied states)
            var (users, userRoles) = await SeedUsersAsync(ctx, ranks, roles);

            // 3. Friendships (pending + accepted)
            await SeedFriendshipsAsync(ctx, users);

            // 4. Communities & Posts
            var communities = await SeedCommunitiesAsync(ctx, users);
            var posts = await SeedPostsAsync(ctx, communities, users);

            // 5. Study Groups, Channels, Memberships, Messages
            var groups = await SeedStudyGroupsAsync(ctx, users);
            var channels = await SeedChannelsAsync(ctx, groups);
            await SeedGroupMembershipsAsync(ctx, groups, users);
            await SeedChannelMessagesAsync(ctx, channels, users);

            // 6. Direct Conversations & Messages
            await SeedDirectConversationsAsync(ctx, users);

            // 7. Notifications (diverse)
            await SeedNotificationsAsync(ctx, users);

            // 8. Learning Resources (doar câteva exemple)
            await SeedLearningResourcesAsync(ctx, users, groups);

            Console.WriteLine("[Seeder] Done.");
        }

        private static async Task ClearAllDataAsync(ApplicationDbContext ctx)
        {
            var tables = new[]
            {
                "AuditLogs", "Reports", "Notifications", "MessageReactions", "MessageAttachments", "Messages",
                "DirectConversationParticipants", "DirectConversations", "PostTags", "PostVotes", "PostViews",
                "PostAttachments", "Posts", "LearningResources", "Events", "GroupInvites", "GroupMemberships",
                "Channels", "ChannelCategories", "GroupRoles", "StudyGroups", "Friendships", "CommunityMemberships",
                "Communities", "StoredFiles", "VerificationRequests", "UserRoles", "UserClaims", "UserLogins",
                "UserTokens", "RoleClaims", "Roles", "Users", "UserRanks", "Tags"
            };
            foreach (var t in tables)
            {
                try { await ctx.Database.ExecuteSqlRawAsync($"DELETE FROM [{t}]"); } catch { }
            }
        }

        private static async Task<List<UserRank>> SeedRanksAsync(ApplicationDbContext ctx)
        {
            var ranks = new List<UserRank>
            {
                new() { Id = Guid.NewGuid(), Name = "Novice", MinPoints = 0, IconUrl = "https://api.dicebear.com/7.x/shapes/svg?seed=novice" },
                new() { Id = Guid.NewGuid(), Name = "Scholar", MinPoints = 1000, IconUrl = "https://api.dicebear.com/7.x/shapes/svg?seed=scholar" },
                new() { Id = Guid.NewGuid(), Name = "Expert", MinPoints = 2500, IconUrl = "https://api.dicebear.com/7.x/shapes/svg?seed=expert" }
            };
            if (!await ctx.UserRanks.AnyAsync())
            {
                await ctx.UserRanks.AddRangeAsync(ranks);
                await ctx.SaveChangesAsync();
            }
            return await ctx.UserRanks.ToListAsync();
        }

        private static async Task<List<IdentityRole>> SeedIdentityRolesAsync(ApplicationDbContext ctx)
        {
            var roles = new List<IdentityRole>
            {
                new() { Id = Guid.NewGuid().ToString(), Name = "Admin", NormalizedName = "ADMIN" },
                new() { Id = Guid.NewGuid().ToString(), Name = "Moderator", NormalizedName = "MODERATOR" },
                new() { Id = Guid.NewGuid().ToString(), Name = "Member", NormalizedName = "MEMBER" }
            };
            await BulkInsertAsync(ctx, roles, "Roles", "Roles");
            return roles;
        }

        private static async Task<(List<ApplicationUser> Users, List<IdentityUserRole<string>> UserRoles)> SeedUsersAsync(ApplicationDbContext ctx, List<UserRank> ranks, List<IdentityRole> roles)
        {
            var hash = new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), DefaultPassword);
            var users = new List<ApplicationUser>();
            var userRoles = new List<IdentityUserRole<string>>();

            var adminRole = roles.First(r => r.Name == "Admin");
            var modRole = roles.First(r => r.Name == "Moderator");
            var memberRole = roles.First(r => r.Name == "Member");

            // Admini
            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "admin@asknlearn.com",
                Email = "admin@asknlearn.com",
                NormalizedEmail = "ADMIN@ASKNLEARN.COM",
                NormalizedUserName = "ADMIN@ASKNLEARN.COM",
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsVerified = true,
                VerificationStatus = UserVerificationStatus.IdentityVerified,
                Role = Role.Admin,
                ReputationPoints = 5000,
                CreatedAt = DateTime.UtcNow.AddMonths(-12),
                PasswordHash = hash,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                AvatarUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=admin"
            };
            users.Add(admin);
            userRoles.Add(new IdentityUserRole<string> { UserId = admin.Id, RoleId = adminRole.Id });

            // Moderatori
            for (int i = 1; i <= 10; i++)
            {
                var mod = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = $"moderator{i}@asknlearn.com",
                    Email = $"moderator{i}@asknlearn.com",
                    NormalizedEmail = $"MODERATOR{i}@ASKNLEARN.COM",
                    NormalizedUserName = $"MODERATOR{i}@ASKNLEARN.COM",
                    FullName = $"Community Moderator {i}",
                    EmailConfirmed = true,
                    IsVerified = true,
                    VerificationStatus = UserVerificationStatus.IdentityVerified,
                    Role = Role.Moderator,
                    ReputationPoints = 3000,
                    CreatedAt = DateTime.UtcNow.AddMonths(-10),
                    PasswordHash = hash,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    AvatarUrl = $"https://api.dicebear.com/7.x/avataaars/svg?seed=moderator{i}"
                };
                users.Add(mod);
                userRoles.Add(new IdentityUserRole<string> { UserId = mod.Id, RoleId = modRole.Id });
            }

            // Alți Admini (total 5)
            for (int i = 2; i <= 5; i++)
            {
                var adm = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = $"admin{i}@asknlearn.com",
                    Email = $"admin{i}@asknlearn.com",
                    NormalizedEmail = $"ADMIN{i}@ASKNLEARN.COM",
                    NormalizedUserName = $"ADMIN{i}@ASKNLEARN.COM",
                    FullName = $"System Administrator {i}",
                    EmailConfirmed = true,
                    IsVerified = true,
                    VerificationStatus = UserVerificationStatus.IdentityVerified,
                    Role = Role.Admin,
                    PasswordHash = hash,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    AvatarUrl = $"https://api.dicebear.com/7.x/avataaars/svg?seed=admin{i}"
                };
                users.Add(adm);
                userRoles.Add(new IdentityUserRole<string> { UserId = adm.Id, RoleId = adminRole.Id });
            }

            // Utilizatori verificați cu email confirmat (100 total)
            string[] firstNames = { "Alex", "Maria", "Andrei", "Elena", "Mihai", "Ioana", "Cristian", "Ana", "Vlad", "Diana", "Stefan", "Laura", "Gabriel", "Andreea", "Matei" };
            string[] lastNames = { "Popescu", "Ionescu", "Dumitrescu", "Stanescu", "Gheorghiu", "Radu", "Marin", "Tudor", "Stoica", "Rusu", "Costin", "Munteanu" };
            for (int i = 0; i < 100; i++)
            {
                var fn = firstNames[Rng.Next(firstNames.Length)];
                var ln = lastNames[Rng.Next(lastNames.Length)];
                var email = $"{fn.ToLower()}.{ln.ToLower()}{i}@stud.ase.ro";
                
                // Userii sunt în general confirmați pentru ușurință la testare, dar unii sunt unverified (unconfirmed email)
                var emailConfirmed = i > 10; // Primii 10 sunt "neconfirmați" pentru testare failure
                var isVerified = emailConfirmed && i % 3 == 0; // 33% sunt verified (student ID)
                
                var u = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = email,
                    Email = email,
                    NormalizedEmail = email.ToUpper(),
                    NormalizedUserName = email.ToUpper(),
                    FullName = $"{fn} {ln}",
                    EmailConfirmed = emailConfirmed,
                    IsVerified = isVerified,
                    VerificationStatus = isVerified ? UserVerificationStatus.IdentityVerified : (emailConfirmed ? UserVerificationStatus.EmailVerified : UserVerificationStatus.NotVerified),
                    Role = Role.Member,
                    ReputationPoints = Rng.Next(50, 2000),
                    CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 400)),
                    PasswordHash = hash,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    AvatarUrl = $"https://api.dicebear.com/7.x/avataaars/svg?seed={fn}{ln}{i}",
                    Bio = $"Student la {Pick(new[] { "ASE", "Poli", "Unibuc", "UMF", "UBB" })}",
                    Institution = Pick(new[] { "ASE Bucuresti", "Universitatea Politehnica", "Universitatea din Bucuresti" })
                };
                users.Add(u);
                userRoles.Add(new IdentityUserRole<string> { UserId = u.Id, RoleId = memberRole.Id });
            }

            await BulkInsertAsync(ctx, users, "Users", "Users");
            await BulkInsertAsync(ctx, userRoles, "UserRoles", "UserRoles");
            return (users, userRoles);
        }

        private static async Task SeedFriendshipsAsync(ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            var friendships = new List<Friendship>();
            var seen = new HashSet<string>();
            foreach (var u in users)
            {
                int friendCount = Rng.Next(10, 30); // Toți au mulți prieteni
                for (int i = 0; i < friendCount; i++)
                {
                    var other = users[Rng.Next(users.Count)];
                    if (other.Id == u.Id) continue;
                    var key = string.Compare(u.Id, other.Id) < 0 ? $"{u.Id}|{other.Id}" : $"{other.Id}|{u.Id}";
                    if (!seen.Add(key)) continue;
                    var status = Rng.NextDouble() > 0.3 ? FriendshipStatus.Accepted : FriendshipStatus.Pending;
                    friendships.Add(new Friendship
                    {
                        RequesterId = u.Id,
                        AddresseeId = other.Id,
                        Status = status,
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 60))
                    });
                }
            }
            await BulkInsertAsync(ctx, friendships, "Friendships", "Friendships");
        }

        private static async Task<List<Community>> SeedCommunitiesAsync(ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            var communities = new List<Community>();
            string[] topics = { "C#", "Java", "Python", "Web Dev", "Mobile", "AI", "DevOps", "CyberSecurity" };
            for (int i = 0; i < 8; i++)
            {
                var creator = users[Rng.Next(users.Count)];
                communities.Add(new Community
                {
                    Id = Guid.NewGuid(),
                    Name = $"{topics[i]} Community",
                    Slug = $"{topics[i].ToLower()}-community",
                    Description = $"Discussions about {topics[i]} programming and technology.",
                    CreatorId = creator.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-Rng.Next(1, 12)),
                    ImageUrl = $"https://api.dicebear.com/7.x/shapes/svg?seed={topics[i]}"
                });
            }
            await BulkInsertAsync(ctx, communities, "Communities", "Communities");
            return communities;
        }

        private static async Task<List<Post>> SeedPostsAsync(ApplicationDbContext ctx, List<Community> communities, List<ApplicationUser> users)
        {
            var posts = new List<Post>();
            foreach (var comm in communities)
            {
                int postCount = Rng.Next(30, 60); // Mai multe postări
                for (int i = 0; i < postCount; i++)
                {
                    var author = users[Rng.Next(users.Count)];
                    var post = new Post
                    {
                        Id = Guid.NewGuid(),
                        CommunityId = comm.Id,
                        AuthorId = author.Id,
                        Title = $"Thread: {topics[communities.IndexOf(comm)]} discussion #{i+1}",
                        Content = $"Let's talk about {topics[communities.IndexOf(comm)]}. Does anyone have advice on this?",
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 120)),
                        ModerationStatus = ModerationStatus.Approved,
                        ViewCount = Rng.Next(50, 2000)
                    };
                    posts.Add(post);
                }
            }
            await BulkInsertAsync(ctx, posts, "Posts", "Posts");

            // SEED COMMENTS (Messages linked to posts)
            var comments = new List<Message>();
            string[] commentTexts = { "Great point!", "I disagree, here is why...", "Thanks for sharing!", "Does this work for everyone?", "Interesting perspective.", "Can you explain more?" };
            foreach (var p in posts.Take(posts.Count / 2)) // Punem comentarii la jumătate din postări
            {
                int commentCount = Rng.Next(5, 15);
                for (int i = 0; i < commentCount; i++)
                {
                    var author = users[Rng.Next(users.Count)];
                    comments.Add(new Message
                    {
                        Id = Guid.NewGuid(),
                        PostId = p.Id,
                        AuthorId = author.Id,
                        Content = commentTexts[Rng.Next(commentTexts.Length)],
                        CreatedAt = p.CreatedAt.AddHours(Rng.Next(1, 48)),
                        ModerationStatus = ModerationStatus.Approved
                    });
                }
            }
            await BulkInsertAsync(ctx, comments, "PostComments", "Messages");
            
            return posts;
        }

        private static async Task<List<StudyGroup>> SeedStudyGroupsAsync(ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            var groups = new List<StudyGroup>();
            string[] subjects = { "Calculus I", "Data Structures", "Macroeconomics", "Distributed Systems", "Machine Learning", "Mobile Apps", "UI/UX Design", "Game Theory", "Cloud Computing" };
            for (int i = 0; i < 20; i++) // 20 groups
            {
                var sub = subjects[Rng.Next(subjects.Length)];
                var owner = users[Rng.Next(users.Count)];
                var isPublic = Rng.NextDouble() > 0.3;
                groups.Add(new StudyGroup
                {
                    Id = Guid.NewGuid(),
                    Name = $"{subjects[i]} Study Group",
                    Description = $"Collaborative learning for {subjects[i]}",
                    SubjectArea = subjects[i],
                    OwnerId = owner.Id,
                    IsPublic = isPublic,
                    CreatedAt = DateTime.UtcNow.AddMonths(-Rng.Next(1, 8)),
                    InviteCode = isPublic ? null : Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
                });
            }
            await BulkInsertAsync(ctx, groups, "StudyGroups", "StudyGroups");
            return groups;
        }

        private static async Task<List<Channel>> SeedChannelsAsync(ApplicationDbContext ctx, List<StudyGroup> groups)
        {
            var channels = new List<Channel>();
            foreach (var g in groups)
            {
                channels.Add(new Channel { Id = Guid.NewGuid(), GroupId = g.Id, Name = "announcements", Type = ChannelType.Text, Position = 0 });
                channels.Add(new Channel { Id = Guid.NewGuid(), GroupId = g.Id, Name = "general", Type = ChannelType.Text, Position = 1 });
                channels.Add(new Channel { Id = Guid.NewGuid(), GroupId = g.Id, Name = "resources-links", Type = ChannelType.Text, Position = 2 });
                channels.Add(new Channel { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Voice Study", Type = ChannelType.Voice, Position = 3 });
                channels.Add(new Channel { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Live Session", Type = ChannelType.Video, Position = 4 });
            }
            await BulkInsertAsync(ctx, channels, "Channels", "Channels");
            return channels;
        }

        private static async Task SeedGroupMembershipsAsync(ApplicationDbContext ctx, List<StudyGroup> groups, List<ApplicationUser> users)
        {
            var memberships = new List<GroupMembership>();
            var roles = new List<GroupRole>();
            var roleDict = new Dictionary<Guid, Guid>(); // GroupId -> MemberRoleId

            foreach (var g in groups)
            {
                var memberRoleId = Guid.NewGuid();
                roles.Add(new GroupRole { Id = memberRoleId, GroupId = g.Id, Name = "Member", Permissions = "READ,WRITE" });
                roleDict[g.Id] = memberRoleId;

                // Owner membership
                if (g.OwnerId != null)
                {
                    memberships.Add(new GroupMembership
                    {
                        GroupId = g.Id,
                        UserId = g.OwnerId,
                        GroupRoleId = memberRoleId,
                        JoinedAt = g.CreatedAt
                    });
                }

                // Alți membri
                int memberCount = Rng.Next(5, 20);
                var potentialMembers = users.Where(u => u.Id != g.OwnerId).ToList();
                for (int i = 0; i < memberCount; i++)
                {
                    var user = potentialMembers[Rng.Next(potentialMembers.Count)];
                    if (memberships.Any(m => m.GroupId == g.Id && m.UserId == user.Id)) continue;
                    memberships.Add(new GroupMembership
                    {
                        GroupId = g.Id,
                        UserId = user.Id,
                        GroupRoleId = memberRoleId,
                        JoinedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 60))
                    });
                }
            }

            await BulkInsertAsync(ctx, roles, "GroupRoles", "GroupRoles");
            await BulkInsertAsync(ctx, memberships, "GroupMemberships", "GroupMemberships");
        }

        private static async Task SeedChannelMessagesAsync(ApplicationDbContext ctx, List<Channel> channels, List<ApplicationUser> users)
        {
            var messages = new List<Message>();
            string[] sampleMessages = {
                "Hello everyone!", "Does anyone have notes for the exam?", "I'll share my summary later.",
                "Great session today!", "What time is the next meeting?", "Check out this resource: example.com",
                "Can someone help with exercise 3?", "I'm stuck on the recursion part.", "Thanks for the help!",
                "Let's schedule a voice call.", "Good luck with your studies!"
            };

            foreach (var ch in channels.Where(c => c.Type == ChannelType.Text))
            {
                int msgCount = Rng.Next(50, 150); // Mult mai multe mesaje pentru scroll
                for (int i = 0; i < msgCount; i++)
                {
                    var author = users[Rng.Next(users.Count)];
                    messages.Add(new Message
                    {
                        Id = Guid.NewGuid(),
                        ChannelId = ch.Id,
                        AuthorId = author.Id,
                        Content = sampleMessages[Rng.Next(sampleMessages.Length)],
                        CreatedAt = DateTime.UtcNow.AddHours(-Rng.Next(1, 72)),
                        ModerationStatus = ModerationStatus.Approved
                    });
                }
            }
            await BulkInsertAsync(ctx, messages, "Messages", "Messages");
        }

        private static async Task SeedDirectConversationsAsync(ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            var conversations = new List<DirectConversation>();
            var participants = new List<DirectConversationParticipant>();
            var messages = new List<Message>();

            // Ia utilizatori care au prietenii acceptate
            var acceptedFriends = ctx.Friendships.Where(f => f.Status == FriendshipStatus.Accepted).ToList();
            var processedPairs = new HashSet<string>();

            foreach (var f in acceptedFriends)
            {
                var pairKey = string.Compare(f.RequesterId, f.AddresseeId) < 0 ? $"{f.RequesterId}|{f.AddresseeId}" : $"{f.AddresseeId}|{f.RequesterId}";
                if (!processedPairs.Add(pairKey)) continue;

                var convId = Guid.NewGuid();
                conversations.Add(new DirectConversation { Id = convId, CreatedAt = f.CreatedAt.AddDays(1) });
                participants.Add(new DirectConversationParticipant { ConversationId = convId, UserId = f.RequesterId });
                participants.Add(new DirectConversationParticipant { ConversationId = convId, UserId = f.AddresseeId });

                // Mesaje
                int msgCount = Rng.Next(40, 100); // Conversații lungi
                for (int i = 0; i < msgCount; i++)
                {
                    var author = i % 2 == 0 ? f.RequesterId : f.AddresseeId;
                    messages.Add(new Message
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = convId,
                        AuthorId = author,
                        Content = $"DM message {i+1} between friends.",
                        CreatedAt = f.CreatedAt.AddDays(1).AddMinutes(i * 15),
                        ModerationStatus = ModerationStatus.Approved
                    });
                }
            }

            await BulkInsertAsync(ctx, conversations, "DirectConversations", "DirectConversations");
            await BulkInsertAsync(ctx, participants, "DirectConversationParticipants", "DirectConversationParticipants");
            await BulkInsertAsync(ctx, messages, "DirectMessages", "Messages");
        }

        private static async Task SeedNotificationsAsync(ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            var notifications = new List<Notification>();
            string[] titles = { "New Connection Request", "Connection Accepted", "New Message", "Post Liked", "Event Reminder" };
            foreach (var u in users.Take(15))
            {
                int count = Rng.Next(2, 8);
                for (int i = 0; i < count; i++)
                {
                    notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = u.Id,
                        Title = titles[Rng.Next(titles.Length)],
                        Message = "You have a new notification.",
                        IsRead = Rng.NextDouble() > 0.5,
                        CreatedAt = DateTime.UtcNow.AddHours(-Rng.Next(1, 48))
                    });
                }
            }
            await BulkInsertAsync(ctx, notifications, "Notifications", "Notifications");
        }

        private static async Task SeedLearningResourcesAsync(ApplicationDbContext ctx, List<ApplicationUser> users, List<StudyGroup> groups)
        {
            var resources = new List<LearningResource>();
            string[] types = { "PDF", "DOCX", "PPT", "ZIP" };
            foreach (var g in groups.Take(3))
            {
                for (int i = 0; i < 3; i++)
                {
                    var uploader = users[Rng.Next(users.Count)];
                    resources.Add(new LearningResource
                    {
                        Id = Guid.NewGuid(),
                        GroupId = g.Id,
                        Title = $"Resource {i+1} for {g.Name}",
                        Description = "Useful study material.",
                        ResourceType = types[Rng.Next(types.Length)],
                        Url = $"https://example.com/resource/{Guid.NewGuid()}",
                        UploaderId = uploader.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 30)),
                        DownloadCount = Rng.Next(5, 100)
                    });
                }
            }
            await BulkInsertAsync(ctx, resources, "LearningResources", "LearningResources");
        }

        private static async Task BulkInsertAsync<T>(ApplicationDbContext ctx, List<T> data, string label, string tableName) where T : class
        {
            if (data.Count == 0) return;
            Console.Write($"[Seeder] {label}: {data.Count} ... ");
            var conn = (SqlConnection)ctx.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            using var bc = new SqlBulkCopy(conn) { DestinationTableName = tableName, BatchSize = 5000, BulkCopyTimeout = 300 };
            var props = typeof(T).GetProperties().Where(p =>
            {
                var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                return p.CanWrite && (t.IsPrimitive || t == typeof(string) || t == typeof(Guid) || t == typeof(DateTime) || t == typeof(decimal) || t.IsEnum);
            }).ToArray();
            foreach (var p in props) bc.ColumnMappings.Add(p.Name, p.Name);
            using var reader = new EntityDataReader<T>(data, props);
            await bc.WriteToServerAsync(reader);
            Console.WriteLine("OK");
        }

        private static T Pick<T>(IReadOnlyList<T> list) => list[Rng.Next(list.Count)];

        private sealed class EntityDataReader<T>(IReadOnlyList<T> data, System.Reflection.PropertyInfo[] props) : IDataReader
        {
            private int _pos = -1;
            public int FieldCount => props.Length;
            public object GetValue(int i)
            {
                var val = props[i].GetValue(data[_pos]);
                if (val != null && (Nullable.GetUnderlyingType(props[i].PropertyType) ?? props[i].PropertyType).IsEnum) return (int)val;
                return val ?? DBNull.Value;
            }
            public bool Read() => ++_pos < data.Count;
            public void Close() { }
            public void Dispose() { }
            public int Depth => 0;
            public bool IsClosed => false;
            public int RecordsAffected => -1;
            public DataTable GetSchemaTable() => null!;
            public bool NextResult() => false;
            public int GetOrdinal(string name) => Array.FindIndex(props, p => p.Name == name);
            public string GetName(int i) => props[i].Name;
            public Type GetFieldType(int i) => props[i].PropertyType;
            public string GetDataTypeName(int i) => GetFieldType(i).Name;
            public object this[int i] => GetValue(i);
            public object this[string name] => GetValue(GetOrdinal(name));
            public bool GetBoolean(int i) => (bool)GetValue(i);
            public byte GetByte(int i) => (byte)GetValue(i);
            public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferOffset, int length) => 0;
            public char GetChar(int i) => (char)GetValue(i);
            public long GetChars(int i, long fieldOffset, char[]? buffer, int bufferOffset, int length) => 0;
            public IDataReader GetData(int i) => null!;
            public DateTime GetDateTime(int i) => (DateTime)GetValue(i);
            public decimal GetDecimal(int i) => (decimal)GetValue(i);
            public double GetDouble(int i) => (double)GetValue(i);
            public float GetFloat(int i) => (float)GetValue(i);
            public Guid GetGuid(int i) => (Guid)GetValue(i);
            public short GetInt16(int i) => (short)GetValue(i);
            public int GetInt32(int i) => (int)GetValue(i);
            public long GetInt64(int i) => (long)GetValue(i);
            public string GetString(int i) => (string)GetValue(i);
            public int GetValues(object[] values) => 0;
            public bool IsDBNull(int i) => GetValue(i) == DBNull.Value;
        }
    }
}