using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.Gamification;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.StudyGroup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Data;
using Microsoft.Data.SqlClient;

namespace AskNLearn.Infrastructure.Persistance
{
    public static class LoadTestDatabaseSeeder
    {
        private sealed record ScaleConfig(
            int UserCount,
            int CommunityCount,
            int GroupCount,
            int BatchSize
        );

        // OPTIMIZARE: 10,000 utilizatori pentru densitate maximă de date
        private static readonly ScaleConfig Cfg = new(
            UserCount:      10_000,
            CommunityCount: 150,
            GroupCount:     250,
            BatchSize:      5_000
        );

        private static readonly Random _globalRng = new();
        private static readonly ThreadLocal<Random> _tlsRng = new(() => new Random(_globalRng.Next()));
        private static Random Rng => _tlsRng.Value!;

        private static readonly string[] FirstNames = ["Andrei","Maria","Alexandru","Elena","Stefan","Ioana","Mihai","Ana","Cristian","Laura","Gabriel","Raluca","Ionut","Diana","Vlad","Andreea"];
        private static readonly string[] LastNames = ["Popescu","Ionescu","Dumitrescu","Stan","Gheorghe","Rusu","Costin","Marin","Tudor","Florescu","Nistor","Dobre"];
        private static readonly string[] Domains = ["@gmail.com","@yahoo.com","@stud.ase.ro","@outlook.com"];
        private static readonly string[] Interests = ["C#","Java","Python","AI","Web Dev","DevOps","Security","Algorithms","Cloud","Mobile Apps"];

        public const string DefaultPassword = "Test@1234!";

        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, bool force = false)
        {
            context.Database.SetCommandTimeout(600);
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"[LoadSeeder] STARTING DENSE SEED (UserCount={Cfg.UserCount})");

            try
            {
                if (force) await ClearAllDataAsync(context);

                var ranks = await SeedRanksAsync(context);
                var tags = await SeedTagsAsync(context, force);
                
                // 1. UTILIZATORI
                var users = await SeedUsersAsync(context, force);
                
                // 2. SOCIAL & COMMUNITY
                var communities = await SeedCommunitiesAsync(context, users, tags, force);
                var posts = await SeedPostsAsync(context, communities, users, tags, force);
                
                // 3. STUDY GROUPS
                var groups = await SeedStudyGroupsAsync(context, users, force);
                await SeedGroupRolesAndMembershipsAsync(context, groups, users, force);
                var channels = await SeedChannelsAsync(context, groups, force);
                
                // 4. MESSAGING & CONTENT (DENS)
                var errors = new List<string>();

                async Task SafeSeed(string name, Func<Task> seedFunc) {
                    try { await seedFunc(); Console.WriteLine($"[LoadSeeder]   {name}: OK"); }
                    catch (Exception ex) { 
                        errors.Add($"**{name}**: {ex.Message}"); 
                        Console.WriteLine($"[LoadSeeder]   {name}: FAILED (Logged)");
                    }
                }

                await SafeSeed("DirectMessages", () => SeeddenseDirectMessagesAsync(context, users));
                await SafeSeed("ChannelMessages", () => SeeddenseChannelMessagesAsync(context, channels, users));
                await SafeSeed("Friendships", () => SeedFriendshipsAsync(context, users, force));
                await SafeSeed("Notifications", () => SeedNotificationsAsync(context, users, force));
                
                // 5. MISC
                await SeedPostVotesAsync(context, posts, users, force);
                await SeedLearningResourcesAsync(context, users, groups, force);

                var totalMessages = await context.Messages.CountAsync();
                Console.WriteLine($"\n[LoadSeeder] VEERIFICATION: {totalMessages} messages total in DB.");

                await GenerateSummaryMarkdown(communities, groups, users);
                
                if (errors.Any()) File.WriteAllText("wwwroot/SeederErrors.md", $"# Seeder Errors\n- {string.Join("\n- ", errors)}");

                sw.Stop();
                Console.WriteLine($"\n[LoadSeeder] COMPLETE în {sw.Elapsed.TotalMinutes:F1} min.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoadSeeder] FATAL ERROR: {ex.Message}");
                try {
                    File.WriteAllText("wwwroot/SeederErrors.md", $"# FATAL SEEDER ERROR\n\n- Message: {ex.Message}\n\n```\n{ex.StackTrace}\n```");
                } catch {}
                throw;
            }
        }

        private static async Task ClearAllDataAsync(ApplicationDbContext ctx)
        {
            var tables = new[] { "AuditLogs", "Reports", "Notifications", "MessageReactions", "Messages", 
                "DirectConversationParticipants", "DirectConversations", "PostTags", "PostVotes", "Posts", 
                "GroupMemberships", "Channels", "ChannelCategories", "GroupRoles", "StudyGroups", 
                "Friendships", "CommunityMemberships", "Communities", "Users" };

            foreach (var t in tables) { try { await ctx.Database.ExecuteSqlRawAsync($"DELETE FROM [{t}]"); } catch {} }
        }

        private static async Task<List<ApplicationUser>> SeedUsersAsync(ApplicationDbContext ctx, bool force)
        {
            var hash = new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), DefaultPassword);
            var users = new List<ApplicationUser>();
            var ranks = await ctx.UserRanks.ToListAsync();

            // Special Accounts
            string[] specialMails = ["admin@asknlearn.com", "moderator@asknlearn.com", "ionut.ungureanu20201@csie.ase.ro", "student@asknlearn.com", "verified@asknlearn.com"];
            foreach(var mail in specialMails)
            {
                var role = mail.Contains("admin") ? Role.Admin : mail.Contains("moderator") ? Role.Moderator : Role.Member;
                users.Add(new ApplicationUser {
                    Id = Guid.NewGuid().ToString(), UserName = mail, Email = mail, NormalizedEmail = mail.ToUpper(), NormalizedUserName = mail.ToUpper(),
                    FullName = mail.Split('@')[0].Replace(".", " "), EmailConfirmed = true, Role = role, ReputationPoints = 1500, IsVerified = mail.Contains("verified") || role != Role.Member,
                    CreatedAt = DateTime.UtcNow.AddMonths(-10), PasswordHash = hash, SecurityStamp = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString()
                });
            }

            var initialCount = users.Count;
            var toSeed = Cfg.UserCount - initialCount;
            for (int i = 0; i < toSeed; i++)
            {
                var fn = Pick(FirstNames);
                var ln = Pick(LastNames);
                var email = $"{fn.ToLower()}.{ln.ToLower()}.{i}@stud.ase.ro";
                users.Add(new ApplicationUser {
                    Id = Guid.NewGuid().ToString(), UserName = email, Email = email, NormalizedEmail = email.ToUpper(), NormalizedUserName = email.ToUpper(),
                    FullName = $"{fn} {ln}", EmailConfirmed = true, Role = Role.Member, ReputationPoints = Rng.Next(10, 5000),
                    IsVerified = Rng.NextDouble() > 0.8, CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 1000)), PasswordHash = hash,
                    SecurityStamp = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString(),
                    AvatarUrl = $"https://api.dicebear.com/7.x/avataaars/svg?seed={fn}{ln}{i}"
                });
            }

            await BulkInsertAsync(ctx, users, "Users", "Users");
            return users;
        }

        private static async Task<List<Community>> SeedCommunitiesAsync(ApplicationDbContext ctx, List<ApplicationUser> users, List<Tag> tags, bool force)
        {
            var staff = users.Where(u => u.Role != Role.Member).ToList();
            var communities = new List<Community>();
            for(int i=0; i<Cfg.CommunityCount; i++)
            {
                var topic = Pick(Interests);
                communities.Add(new Community {
                    Id = Guid.NewGuid(), Name = $"{topic} Hub #{i}", Slug = $"{topic.ToLower()}-{i}", Description = $"Comunitate pentru {topic}",
                    CreatorId = Pick(staff).Id, CreatedAt = DateTime.UtcNow.AddMonths(-Rng.Next(1, 24)), ImageUrl = $"https://api.dicebear.com/7.x/shapes/svg?seed={i}"
                });
            }
            await BulkInsertAsync(ctx, communities, "Communities", "Communities");

            // Memberships DENSE
            var memberships = new List<CommunityMembership>();
            var seen = new HashSet<string>();
            foreach(var user in users)
            {
                var joined = PickMany(communities, Rng.Next(5, 12));
                foreach(var c in joined) {
                    if(seen.Add($"{c.Id}|{user.Id}"))
                        memberships.Add(new CommunityMembership { CommunityId = c.Id, UserId = user.Id, Role = CommunityRole.Member, JoinedAt = DateTime.UtcNow.AddDays(-10) });
                }
            }
            await BulkInsertAsync(ctx, memberships, "CommunityMemberships", "CommunityMemberships");
            return communities;
        }

        private static async Task<List<StudyGroup>> SeedStudyGroupsAsync(ApplicationDbContext ctx, List<ApplicationUser> users, bool force)
        {
            var groups = new List<StudyGroup>();
            for(int i=0; i<Cfg.GroupCount; i++)
            {
                var topic = Pick(Interests);
                groups.Add(new StudyGroup {
                    Id = Guid.NewGuid(), Name = $"{topic} Group {i}", Description = $"Grup de studiu axat pe {topic}",
                    OwnerId = Pick(users).Id, IsPublic = true, SubjectArea = topic, CreatedAt = DateTime.UtcNow.AddMonths(-Rng.Next(1, 12))
                });
            }
            await BulkInsertAsync(ctx, groups, "StudyGroups", "StudyGroups");
            return groups;
        }

        private static async Task SeedGroupRolesAndMembershipsAsync(ApplicationDbContext ctx, List<StudyGroup> groups, List<ApplicationUser> users, bool force)
        {
            var roles = new List<GroupRole>();
            var memberships = new List<GroupMembership>();
            foreach(var g in groups)
            {
                var rid = Guid.NewGuid();
                roles.Add(new GroupRole { Id = rid, GroupId = g.Id, Name = "Member", Permissions = "READ,WRITE" });
                
                if (g.OwnerId != null) memberships.Add(new GroupMembership { GroupId = g.Id, UserId = g.OwnerId, GroupRoleId = rid, JoinedAt = g.CreatedAt });
                
                var members = PickMany(users.Where(u => u.Id != g.OwnerId).ToList(), Rng.Next(15, 60));
                foreach(var m in members) memberships.Add(new GroupMembership { GroupId = g.Id, UserId = m.Id, GroupRoleId = rid, JoinedAt = DateTime.UtcNow.AddDays(-5) });
            }
            await BulkInsertAsync(ctx, roles, "GroupRoles", "GroupRoles");
            await BulkInsertAsync(ctx, memberships, "GroupMemberships", "GroupMemberships");
        }

        private static async Task<List<Channel>> SeedChannelsAsync(ApplicationDbContext ctx, List<StudyGroup> groups, bool force)
        {
            var channels = new List<Channel>();
            foreach(var g in groups) {
                channels.Add(new Channel { Id = Guid.NewGuid(), GroupId = g.Id, Name = "general", Type = ChannelType.Text, Position = 0 });
                channels.Add(new Channel { Id = Guid.NewGuid(), GroupId = g.Id, Name = "resources", Type = ChannelType.Text, Position = 1 });
            }
            await BulkInsertAsync(ctx, channels, "Channels", "Channels");
            return channels;
        }

        private static async Task SeeddenseChannelMessagesAsync(ApplicationDbContext ctx, List<Channel> channels, List<ApplicationUser> users)
        {
            var messages = new List<Message>();
            var topics = new Dictionary<string, string[]> {
                ["AI"] = ["Care este diferența dintre CNN și RNN?", "A încercat cineva noile modele de la HuggingFace?", "Deep Learning pare viitorul în Computer Vision.", "Am nevoie de ajutor cu un gradient descent manual.", "Transformer-ele au revoluționat complet NLP-ul."],
                ["C#"] = ["LINQ este absolut genial pentru procesarea colecțiilor.", "Când folosim record în loc de class în C# 9+?", "Async/Await poate fi periculos dacă nu înțelegi thread-urile.", "Dependency Injection face codul mult mai ușor de testat.", "Entity Framework Core a evoluat mult în versiunea 8."],
                ["Java"] = ["Spring Boot simplifică mult configurarea microserviciilor.", "Care este legătura dintre JVM și Bytecode?", "Streams API în Java 8 a schimbat totul.", "Hibernate vs JPA: ce preferați pentru proiecte medii?", "Garbage collection-ul poate fi optimizat manual?"],
                ["Python"] = ["Pandas este indispensabil pentru analiza datelor.", "De ce preferați Fast API în loc de Flask?", "List comprehensions fac codul Pythonic.", "AI-ul fără Python ar fi fost mult mai greu de implementat.", "Cum gestionați mediile virtuale (venv vs conda)?"],
                ["Security"] = ["Am găsit o vulnerabilitate XSS pe un site de test.", "Cât de siguri sunt algoritmii de hashing precum SHA-256?", "Penetration testing-ul necesită multă disciplină.", "Zero Trust Architecture este noul standard.", "Criptografia asimetrică este fascinantă."],
                ["Web"] = ["React vs Angular: bătălia continuă și în 2026.", "Tailwind CSS accelerează mult dezvoltarea UI-ului.", "Next.js 15 aduce îmbunătățiri mari la caching.", "WebAssembly ar putea înlocui JS în anumite scenarii?", "Vercel face deployment-ul o joacă de copii."],
                ["Generic"] = ["Salutare tuturor! Spor la învățat.", "Am postat o resursă nouă în canalul dedicat.", "Când avem următoarea sesiune de voice?", "Proiectul de echipă merge conform planului.", "Aveți resurse bune pentru examenul de mâine?"]
            };

            foreach(var c in channels)
            {
                var groupName = c.Group?.Name ?? "";
                var groupSubject = c.Group?.SubjectArea ?? "";
                var currentTopic = topics.Keys.FirstOrDefault(k => groupName.Contains(k, StringComparison.OrdinalIgnoreCase) || groupSubject.Contains(k, StringComparison.OrdinalIgnoreCase)) ?? "Generic";
                var templates = topics[currentTopic];

                var count = Rng.Next(15, 30);
                for(int i=0; i<count; i++) {
                    var content = Pick(templates);
                    messages.Add(new Message { 
                        Id = Guid.NewGuid(), 
                        ChannelId = c.Id, 
                        AuthorId = Pick(users).Id, 
                        Content = content, 
                        CreatedAt = DateTime.UtcNow.AddHours(-Rng.Next(1, 48)), 
                        ModerationStatus = ModerationStatus.Approved 
                    });
                }
            }
            await BulkInsertAsync(ctx, messages, "ChannelMessages", "Messages");
        }

        private static async Task SeeddenseDirectMessagesAsync(ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            var conversations = new List<DirectConversation>();
            var participants = new List<DirectConversationParticipant>();
            var messages = new List<Message>();
            
            var sample = users.Take(1000).ToList();
            for(int i=0; i<sample.Count; i+=2)
            {
                var u1 = sample[i]; var u2 = sample[i+1];
                var cid = Guid.NewGuid();
                conversations.Add(new DirectConversation { Id = cid, CreatedAt = DateTime.UtcNow.AddDays(-20) });
                participants.Add(new DirectConversationParticipant { ConversationId = cid, UserId = u1.Id });
                participants.Add(new DirectConversationParticipant { ConversationId = cid, UserId = u2.Id });

                for(int m=0; m<15; m++) {
                    messages.Add(new Message { Id = Guid.NewGuid(), ConversationId = cid, AuthorId = (m%2==0?u1.Id:u2.Id), Content = $"Salut! Mesaj DM #{m}", CreatedAt = DateTime.UtcNow.AddMinutes(m*15), ModerationStatus = ModerationStatus.Approved });
                }
            }
            await BulkInsertAsync(ctx, conversations, "DirectConversations", "DirectConversations");
            await BulkInsertAsync(ctx, participants, "DirectParticipants", "DirectConversationParticipants");
            await BulkInsertAsync(ctx, messages, "DirectMessages", "Messages");
        }

        private static async Task SeedFriendshipsAsync(ApplicationDbContext ctx, List<ApplicationUser> users, bool force)
        {
            var friendships = new List<Friendship>();
            var seen = new HashSet<string>();
            var sample = users.Take(3000).ToList();

            foreach(var u in sample)
            {
                var fCount = Rng.Next(5, 15);
                for(int i=0; i<fCount; i++) {
                    var other = Pick(users);
                    if(other.Id == u.Id) continue;
                    var key = string.Compare(u.Id, other.Id) < 0 ? $"{u.Id}|{other.Id}" : $"{other.Id}|{u.Id}";
                    if(seen.Add(key)) friendships.Add(new Friendship { RequesterId = u.Id, AddresseeId = other.Id, Status = FriendshipStatus.Accepted, CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 100)) });
                }
            }
            await BulkInsertAsync(ctx, friendships, "Friendships", "Friendships");
        }

        private static async Task<List<Post>> SeedPostsAsync(ApplicationDbContext ctx, List<Community> communities, List<ApplicationUser> users, List<Tag> tags, bool force)
        {
            var posts = new List<Post>();
            foreach(var comm in communities)
            {
                var count = Rng.Next(15, 40);
                for(int i=0; i<count; i++) {
                    posts.Add(new Post { Id = Guid.NewGuid(), CommunityId = comm.Id, AuthorId = Pick(users).Id, Title = $"Thread #{i} topic {comm.Name}", Content = $"Discutie despre {comm.Name} continut...", CreatedAt = DateTime.UtcNow.AddDays(-i), ModerationStatus = ModerationStatus.Approved });
                }
            }
            await BulkInsertAsync(ctx, posts, "Posts", "Posts");
            return posts;
        }

        private static async Task SeedNotificationsAsync(ApplicationDbContext ctx, List<ApplicationUser> users, bool force)
        {
            var notifs = new List<Notification>();
            foreach(var u in users.Take(2000)) {
                for(int i=0; i<5; i++) notifs.Add(new Notification { Id = Guid.NewGuid(), UserId = u.Id, Title = "Notificare noua", Message = "Ai un mesaj nou sau o activitate.", IsRead = i > 2, CreatedAt = DateTime.UtcNow.AddHours(-i) });
            }
            await BulkInsertAsync(ctx, notifs, "Notifications", "Notifications");
        }

        private static async Task SeedPostVotesAsync(ApplicationDbContext ctx, List<Post> posts, List<ApplicationUser> users, bool force)
        {
            var votes = new List<PostVote>();
            var seen = new HashSet<string>();
            foreach(var p in posts.Take(Cfg.BatchSize)) {
                var voters = PickMany(users, Rng.Next(5, 30));
                foreach(var v in voters) {
                    if(seen.Add($"{p.Id}|{v.Id}"))
                        votes.Add(new PostVote { PostId = p.Id, UserId = v.Id, VoteValue = (short)(Rng.Next(2)==0?1:-1) });
                }
            }
            await BulkInsertAsync(ctx, votes, "PostVotes", "PostVotes");
        }

        private static async Task SeedLearningResourcesAsync(ApplicationDbContext ctx, List<ApplicationUser> users, List<StudyGroup> groups, bool force)
        {
            var res = new List<LearningResource>();
            foreach(var g in groups.Take(50)) {
                for(int i=1; i<=3; i++) res.Add(new LearningResource { Id = Guid.NewGuid(), GroupId = g.Id, UploaderId = Pick(users).Id, Title = $"Resursa {i} {g.Name}", Description = "Material suport.", ResourceType = "PDF", Url = "#", CreatedAt = DateTime.UtcNow });
            }
            await BulkInsertAsync(ctx, res, "LearningResources", "LearningResources");
        }

        private static async Task<List<Tag>> SeedTagsAsync(ApplicationDbContext ctx, bool force)
        {
            if (force) {
                ctx.Tags.RemoveRange(ctx.Tags);
                await ctx.SaveChangesAsync();
            }

            var tags = Interests.Select(n => new Tag { Id = Guid.NewGuid(), Name = n }).ToList();
            if(!await ctx.Tags.AnyAsync()) { await ctx.Tags.AddRangeAsync(tags); await ctx.SaveChangesAsync(); }
            return await ctx.Tags.ToListAsync();
        }

        private static async Task<List<UserRank>> SeedRanksAsync(ApplicationDbContext ctx)
        {
            var ranks = new List<UserRank> { new() { Id = Guid.NewGuid(), Name = "Novice", MinPoints = 0 }, new() { Id = Guid.NewGuid(), Name = "Scholar", MinPoints = 1000 } };
            if(!await ctx.UserRanks.AnyAsync()) { await ctx.UserRanks.AddRangeAsync(ranks); await ctx.SaveChangesAsync(); }
            return ranks;
        }

        private static async Task BulkInsertAsync<T>(ApplicationDbContext ctx, List<T> data, string label, string tableName) where T : class
        {
            if (data.Count == 0) return;
            var sw = Stopwatch.StartNew();
            Console.Write($"[LoadSeeder]   {label}: {data.Count:N0}...");
            var conn = (SqlConnection)ctx.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            using var bc = new SqlBulkCopy(conn) { DestinationTableName = tableName, BatchSize = 10000, BulkCopyTimeout = 600 };
            var props = typeof(T).GetProperties().Where(p => {
                var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                return p.CanWrite && (t.IsPrimitive || t == typeof(string) || t == typeof(Guid) || t == typeof(DateTime) || t == typeof(decimal) || t.IsEnum);
            }).ToArray();
            foreach (var p in props) bc.ColumnMappings.Add(p.Name, p.Name);
            using var reader = new EntityDataReader<T>(data, props);
            await bc.WriteToServerAsync(reader);
            Console.WriteLine($" OK ({sw.Elapsed.TotalSeconds:F1}s)");
        }

        private static T Pick<T>(IReadOnlyList<T> list) => list[Rng.Next(list.Count)];
        private static List<T> PickMany<T>(List<T> list, int n) => list.OrderBy(_ => Rng.Next()).Take(n).ToList();

        private static async Task GenerateSummaryMarkdown(List<Community> communities, List<StudyGroup> groups, List<ApplicationUser> users)
        {
            try {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "SeedSummary.md");
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("# AskNLearn - Seed Summary Report");
                sb.AppendLine($"Generated on: {DateTime.Now:f}\n");
                sb.AppendLine("## 🧪 Test Accounts (Password: `Test@1234!`)");
                sb.AppendLine("| Role | Email | Status |");
                sb.AppendLine("|---|---|---|");
                sb.AppendLine("| Admin | admin@asknlearn.com | Verified |");
                sb.AppendLine("| Moderator | moderator@asknlearn.com | Verified |");
                sb.AppendLine("| Student (Test) | ionut.ungureanu20201@csie.ase.ro | Verified |");
                sb.AppendLine("| Normal Student | student@asknlearn.com | Standard |");
                sb.AppendLine("| Verified Student | verified@asknlearn.com | Identity Verified |\n");
                
                sb.AppendLine("## 🏛️ Top Communities & Creators");
                foreach(var c in communities.Take(10)) {
                    var owner = users.Find(u => u.Id == c.CreatorId);
                    sb.AppendLine($"- **{c.Name}** (Slug: `{c.Slug}`) - Created by: {owner?.Email}");
                }
                
                sb.AppendLine("\n## 📚 Top Study Groups & Owners");
                foreach(var g in groups.Take(10)) {
                    var owner = users.Find(u => u.Id == g.OwnerId);
                    sb.AppendLine($"- **{g.Name}** - Owned by: {owner?.Email}");
                }

                await File.WriteAllTextAsync(path, sb.ToString());
                Console.WriteLine($"[LoadSeeder] SUMMARY WRITTEN TO: {path}");
            } catch {}
        }

        private sealed class EntityDataReader<T>(IReadOnlyList<T> data, System.Reflection.PropertyInfo[] props) : IDataReader
        {
            private int _pos = -1;
            public int FieldCount => props.Length;
            public object GetValue(int i) {
                var val = props[i].GetValue(data[_pos]);
                if (val != null && (Nullable.GetUnderlyingType(props[i].PropertyType) ?? props[i].PropertyType).IsEnum) return (int)val;
                return val ?? DBNull.Value;
            }
            public bool Read() => ++_pos < data.Count;
            public void Close() {} public void Dispose() {} public int Depth => 0; public bool IsClosed => false;
            public int RecordsAffected => -1; public DataTable GetSchemaTable() => null!; public bool NextResult() => false;
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