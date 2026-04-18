using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.Gamification;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Domain.Entities.SocialFeed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.SqlClient;
using ModerationStatus = AskNLearn.Domain.Entities.Core.ModerationStatus;

namespace AskNLearn.Infrastructure.Persistance
{
    public static class LoadTestDatabaseSeeder
    {
        public const string DefaultPassword = "Test@1234!";

        private static readonly Random _globalRng = new();
        private static readonly ThreadLocal<Random> _tlsRng = new(() => new Random(_globalRng.Next()));
        private static Random Rng => _tlsRng.Value!;

        private static readonly string[] _topics = { "C#", "Java", "Python", "Web Dev", "Mobile", "AI", "DevOps", "CyberSecurity" };

        public static async Task SeedAsync(ApplicationDbContext ctx, UserManager<ApplicationUser> userManager, bool force = false)
        {
            ctx.Database.SetCommandTimeout(600);
            Console.WriteLine("[Seeder] Starting comprehensive seed...");

            // Ensure StoredFiles has the new columns (Manual Migration Hack)
            try
            {
                var conn = ctx.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                
                await ctx.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[StoredFiles]') AND name = 'IsSafe')
                    BEGIN
                        ALTER TABLE [StoredFiles] ADD [IsSafe] BIT NOT NULL DEFAULT 1;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[StoredFiles]') AND name = 'SecurityNotes')
                    BEGIN
                        ALTER TABLE [StoredFiles] ADD [SecurityNotes] NVARCHAR(MAX) NULL;
                    END
                ");
            }
            catch { /* Ignore if it fails or if it's not SQL Server */ }


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


            // 6. Direct Conversations & Messages
            await SeedDirectConversationsAsync(ctx, users);

            // 7. Notifications (diverse)
            await SeedNotificationsAsync(ctx, users);


            Console.WriteLine("[Seeder] Done.");
        }

        private static async Task ClearAllDataAsync(ApplicationDbContext ctx)
        {
            var tables = new[]
            {
                "AuditLogs", "Reports", "Notifications", "MessageReactions", "MessageAttachments", "Messages",
                "DirectConversationParticipants", "DirectConversations", "PostTags", "PostVotes", "PostViews",
                "PostAttachments", "Posts",
                "StoredFiles", "VerificationRequests", "UserRoles", "UserClaims", "UserLogins",
                "UserTokens", "RoleClaims", "Roles", "Users", "UserRanks", "Tags"
            };
            foreach (var t in tables)
            {
                try { await ctx.Database.ExecuteSqlAsync($"DELETE FROM [{t}]"); } catch { }
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
            if (await ctx.Roles.AnyAsync()) return await ctx.Roles.ToListAsync();

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
            if (await ctx.Users.AnyAsync()) 
            {
                var existingUsers = await ctx.Users.ToListAsync();
                var existingUserRoles = await ctx.UserRoles.ToListAsync();
                return (existingUsers, existingUserRoles);
            }
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
            if (await ctx.Friendships.AnyAsync()) return;
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
            if (await ctx.Communities.AnyAsync()) return await ctx.Communities.ToListAsync();
            var communities = new List<Community>();
            for (int i = 0; i < 8; i++)
            {
                var creator = users[Rng.Next(users.Count)];
                communities.Add(new Community
                {
                    Id = Guid.NewGuid(),
                    Name = $"{_topics[i]} Community",
                    Slug = $"{_topics[i].ToLower()}-community",
                    Description = $"Discussions about {_topics[i]} programming and technology.",
                    CreatorId = creator.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-Rng.Next(1, 12)),
                    ImageUrl = $"https://api.dicebear.com/7.x/shapes/svg?seed={_topics[i]}"
                });
            }
            await BulkInsertAsync(ctx, communities, "Communities", "Communities");
            return communities;
        }

        private static async Task<List<Post>> SeedPostsAsync(ApplicationDbContext ctx, List<Community> communities, List<ApplicationUser> users)
        {
            if (await ctx.Posts.AnyAsync()) return await ctx.Posts.ToListAsync();
            var posts = new List<Post>();
            string[] postTitles = { "How to implement JWT in ASP.NET Core?", "Looking for study partners for the Midterm Exam", "Has anyone tried the new Python library for ML?", "Discussion: Microservices vs Monoliths in 2025", "Best resources for learning Distributed Systems", "Internship opportunities at tech companies this summer" };
            string[] postContents = { 
                "I've been trying to set up JWT authentication in my latest project but I'm running into some issues with token expiration. Does anyone have a good tutorial or sample code?",
                "The midterm exam for Advanced Algorithms is coming up next week. I'm struggling with Dynamic Programming. Anyone interested in a group study session at the library?",
                "I just found this amazing library called 'SciPy-Fast' that claims to be 10x faster for matrix multiplications. Has anyone used it in production yet?",
                "There's a lot of talk about moving back to monoliths for simpler deployment. What do you think is the right approach for a student project?",
                "I'm looking for some advanced books on Distributed Systems. I've already read DDIA by Martin Kleppmann. Any other suggestions?",
                "I saw that several companies are opening their internship portals this week. Let's share links and tips for the interview process!"
            };

            foreach (var comm in communities)
            {
                int postCount = Rng.Next(30, 60);
                for (int i = 0; i < postCount; i++)
                {
                    var author = users[Rng.Next(users.Count)];
                    var titleIdx = Rng.Next(postTitles.Length);
                    var post = new Post
                    {
                        Id = Guid.NewGuid(),
                        CommunityId = comm.Id,
                        AuthorId = author.Id,
                        Title = postTitles[titleIdx],
                        Content = postContents[titleIdx] + "\n\nThis is a long-form discussion post aimed at improving our collective knowledge in this specific field. Please contribute your thoughts below!",
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 120)),
                        ModerationStatus = AskNLearn.Domain.Entities.Core.ModerationStatus.Approved,
                        ViewCount = Rng.Next(50, 2000)
                    };
                    posts.Add(post);
                }
            }
            await BulkInsertAsync(ctx, posts, "Posts", "Posts");

            // SEED COMMENTS (Messages linked to posts)
            var comments = new List<Message>();
            string[] commentTexts = { 
                "That's a very interesting point. I think the key is to focus on scalability from the start.", 
                "I've had a similar issue before. Usually, it's related to the CORS configuration in the Startup class.", 
                "Thanks for sharing this! I'll definitely check out the library you mentioned.", 
                "I'm in for the study session! Does Wednesday afternoon work for everyone?", 
                "I actually disagree. Monoliths are better for small teams because they reduce operational complexity significantly.", 
                "Could you provide more details on how you solved the authentication bug? I'm stuck on the same thing." 
            };
            foreach (var p in posts.Take(posts.Count / 2))
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
                        ModerationStatus = AskNLearn.Domain.Entities.Core.ModerationStatus.Approved
                    });
                }
            }
            await BulkInsertAsync(ctx, comments, "PostComments", "Messages");
            
            return posts;
        }


        private static async Task SeedDirectConversationsAsync(ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            if (await ctx.DirectConversations.AnyAsync()) return;
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
                int msgCount = Rng.Next(50, 150); // Conversații chiar mai lungi
                string[] dmStarters = { "Hey, do you have the notes from today's course?", "I was thinking about that project we discussed.", "Did you see the latest announcement?", "I'm struggling with the C# assignment, any tips?", "Are you coming to the study session tomorrow?" };
                string[] dmResponses = { "Yeah, I'll send them in a bit.", "I think we should focus on the database layer first.", "No, what happened?", "Sure, I can help you with that. Which part is tricky?", "I'll be there, see you then!" };
                
                for (int i = 0; i < msgCount; i++)
                {
                    var author = i % 2 == 0 ? f.RequesterId : f.AddresseeId;
                    var content = i == 0 ? dmStarters[Rng.Next(dmStarters.Length)] : 
                                  i == 1 ? dmResponses[Rng.Next(dmResponses.Length)] :
                                  $"Message {i+1}: This is part of a longer academic discussion about topics like software architecture, distributed systems, and modern web development. We are collaborating on a research project for the Computer Science department.";
                    
                    messages.Add(new Message
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = convId,
                        AuthorId = author,
                        Content = content,
                        CreatedAt = f.CreatedAt.AddDays(1).AddMinutes(i * 15),
                        ModerationStatus = AskNLearn.Domain.Entities.Core.ModerationStatus.Approved
                    });
                }
            }

            await BulkInsertAsync(ctx, conversations, "DirectConversations", "DirectConversations");
            await BulkInsertAsync(ctx, participants, "DirectConversationParticipants", "DirectConversationParticipants");
            await BulkInsertAsync(ctx, messages, "DirectMessages", "Messages");

            // SEED RESOURCES
            await SeedResourcesAsync(ctx, users);
        }

        private static async Task SeedResourcesAsync(ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            if (await ctx.StoredFiles.AnyAsync(f => f.ModuleContext == "Resources")) return;
            var resources = new List<StoredFile>();
            string[] fileNames = { "Course_Notes_Week1.pdf", "Algorithm_CheatSheet.docx", "Project_Proposal.pdf", "Exam_Prep_2025.pdf", "Database_Schema_v2.png" };
            for (int i = 0; i < 20; i++)
            {
                var uploader = users[Rng.Next(users.Count)];
                var name = fileNames[Rng.Next(fileNames.Length)];
                resources.Add(new StoredFile
                {
                    Id = Guid.NewGuid(),
                    FileName = $"{i}_{name}",
                    FilePath = $"/uploads/resources/sample_{i}.pdf",
                    FileType = name.EndsWith(".pdf") ? "application/pdf" : name.EndsWith(".png") ? "image/png" : "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    FileSize = Rng.Next(1024, 1024 * 1024 * 5),
                    UploaderId = uploader.Id,
                    UploadedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 30)),
                    ModuleContext = "Resources"
                });
            }
            await BulkInsertAsync(ctx, resources, "Resources", "StoredFiles");
        }

        private static async Task SeedNotificationsAsync(ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            if (await ctx.Notifications.AnyAsync()) return;
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