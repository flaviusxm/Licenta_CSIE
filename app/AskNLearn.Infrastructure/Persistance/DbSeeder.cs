using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.Gamification;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.StudyGroup;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;

namespace AskNLearn.Infrastructure.Persistance
{
    public static class LoadTestDatabaseSeeder
    {
        // ─── CONFIG ───────────────────────────────────────────────────────────────

        private sealed record ScaleConfig(
            int UserCount,
            int PostsPerCommunity,
            int PostsPerChannel,
            int CommentsPerPost,
            int ChannelMessagesPerChannel,
            int MaxVotersPerPost,
            int MaxViewersPerPost,
            int DirectConvCount,
            int AuditLogCount,
            int PostReportCount,
            int NotifPerUser,
            int ResourcesPerGroup,
            int EventsPerCommunity,
            int InvitesPerGroup,
            int BatchSize
        );

        private static readonly ScaleConfig Cfg = new(
            UserCount:                100_000,
            PostsPerCommunity:        200,
            PostsPerChannel:          50,
            CommentsPerPost:          10,
            ChannelMessagesPerChannel:100,
            MaxVotersPerPost:         200,
            MaxViewersPerPost:        500,
            DirectConvCount:          5_000,
            AuditLogCount:            5_000,
            PostReportCount:          2_000,
            NotifPerUser:             5,
            ResourcesPerGroup:        20,
            EventsPerCommunity:       10,
            InvitesPerGroup:          20,
            BatchSize:                10_000
        );

        // ─── RANDOM ───────────────────────────────────────────────────────────────

        private static readonly Random _globalRng = new();
        private static readonly ThreadLocal<Random> _tlsRng =
            new(() => new Random(_globalRng.Next()));
        private static Random Rng => _tlsRng.Value!;

        // ─── FIXTURES ─────────────────────────────────────────────────────────────

        private static readonly string[] FirstNames =
            ["Andrei","Maria","Alexandru","Elena","Stefan","Ioana","Mihai","Ana",
             "Cristian","Laura","Gabriel","Raluca","Ionut","Diana","Vlad","Andreea",
             "Matei","Simona","Claudiu","Monica","Bogdan","Teodora","Radu","Alina",
             "Cosmin","Bianca","Florin","Catalina","Sorin","Mirela",
             "Octavian","Luminita","Dragos","Roxana","Silviu","Corina","Adrian","Petronela"];

        private static readonly string[] LastNames =
            ["Popescu","Ionescu","Dumitrescu","Stan","Gheorghe","Rusu","Costin","Marin",
             "Tudor","Florescu","Nistor","Dobre","Barbu","Mihaila","Radulescu","Voinea",
             "Matei","Cristea","Diaconu","Enache","Badea","Iordache","Bucur","Vasilescu",
             "Lazar","Grigore","Ciobanu","Avram","Zaharia","Stanescu",
             "Ungureanu","Constantin","Neagu","Stoica","Manea","Olaru","Popa","Serban"];

        private static readonly string[] Domains =
            ["@gmail.com","@yahoo.com","@outlook.com","@stud.ase.ro",
             "@csie.ase.ro","@upb.ro","@unibuc.ro","@student.ubbcluj.ro"];

        private static readonly string[] Occupations =
            ["Student","Master Student","PhD Student","Lecturer",
             "Researcher","Teaching Assistant","Industry Engineer","Junior Developer","Senior Developer"];

        private static readonly string[] Institutions =
            ["ASE Bucuresti","Politehnica Bucuresti","Universitatea din Bucuresti",
             "SNSPA","Academia Navala","Universitatea Babes-Bolyai","Universitatea Tehnica Cluj",
             "Universitatea de Vest Timisoara","Universitatea Alexandru Ioan Cuza Iasi"];

        private static readonly string[] Interests =
            ["C#","Java","Python","Machine Learning","Web Development","DevOps",
             "Databases","Cybersecurity","Game Development","Mobile Apps","Cloud",
             "Algorithms","Data Science","IoT","Blockchain","Rust","Go","Kotlin","Swift"];

        private static readonly string[] PostTitles =
            ["Cum rezolv problema N+1 in Entity Framework?",
             "Best practices pentru autentificare JWT in .NET 8",
             "Diferenta dintre IQueryable si IEnumerable",
             "Index optimization in PostgreSQL - ghid complet",
             "CQRS vs Repository Pattern - ce alegi?",
             "Machine Learning cu Python - resurse pentru incepatori",
             "Proiect de licenta - alegere subiect AI sau Web?",
             "Cum scriu o lucrare academica buna?",
             "Microservices vs Monolith pentru proiecte universitare",
             "Git branching strategy pentru echipe mici",
             "Docker Compose pentru development local",
             "Redis caching in ASP.NET Core",
             "Optimizare query-uri SQL cu EXPLAIN ANALYZE",
             "Cum implementez un sistem de notificari real-time?",
             "Clean Architecture in .NET 8 - tutorial complet",
             "CI/CD cu GitHub Actions pentru proiecte .NET",
             "Testare unitara cu xUnit si Moq",
             "Blazor vs React - ce sa aleg pentru frontend?",
             "SignalR WebSockets in ASP.NET Core",
             "GraphQL vs REST - comparatie detaliata"];

        private static readonly string[] PostContents =
            ["Am intalnit aceasta problema in proiectul meu si nu stiu cum sa o rezolv. Orice ajutor este binevenit!",
             "Dupa ce am studiat documentatia oficiala, am ajuns la concluzia ca cea mai buna abordare este urmatoarea...",
             "In cadrul cursului nostru, profesorul a mentionat aceasta tehnica dar nu a intrat in detalii. Cine poate explica?",
             "Am finalizat un proiect folosind aceasta tehnologie si pot sa impartasesc experienta mea cu voi.",
             "Cautam colegi pentru un proiect de grup la materia Baze de Date. Avem nevoie de 2-3 persoane.",
             "Am gasit o solutie eleganta pentru aceasta problema si vreau sa o impartasesc cu comunitatea.",
             "Cineva are resurse recomandate pentru a aprofunda acest subiect? Am nevoie de exemple practice.",
             "Aceasta este o intrebare mai avansata despre arhitectura sistemelor distribuite...",
             "Dupa multiple incercari, am reusit sa rezolv bug-ul. Iata pasii pe care i-am urmat.",
             "Vreau sa discut despre best practices in domeniu. Care este experienta voastra?"];

        private static readonly string[] CommentContents =
            ["Super explicatie, multumesc!",
             "Am incercat si la mine merge!",
             "Poti sa dai si un exemplu de cod?",
             "Eu am intalnit aceeasi problema saptamana trecuta.",
             "Nu sunt de acord complet, exista si alte aspecte de luat in considerare.",
             "Excelent post! Il salvez pentru referinta.",
             "Am mai multe intrebari, pot sa vin la consultatie?",
             "Confirm, am testat pe PostgreSQL 15 si functioneaza.",
             "Update: am rezolvat, era o problema de configurare.",
             "Mersi pentru indicatie! M-a ajutat enorm.",
             "Ai putea sa detaliezi pasul 3? Nu inteleg exact ce vrei sa spui.",
             "Am implementat ceva similar in proiectul meu de licenta.",
             "Recomanzi vreo carte sau curs online pe acest subiect?",
             "Exact asta cautam! Multumesc pentru resursa.",
             "Am o abordare alternativa care ar putea fi mai eficienta..."];

        private static readonly string[] ChannelMsgContents =
            ["Salut tuturor! Bine am gasit grupul!",
             "Cand avem urmatoarea sedinta de studiu?",
             "Am incarcat niste resurse noi in sectiunea de materiale.",
             "Reminder: maine avem deadline pentru tema!",
             "Cineva poate explica notiunile din capitolul 3?",
             "Am rezolvat exercitiul 5, vreti sa il discut?",
             "Multumesc pentru ajutor, am inteles acum!",
             "Propun o sesiune de recapitulare vineri seara.",
             "Succes tuturor la examene!",
             "Felicitari echipei pentru release-ul de ieri!",
             "Cine vine la laboratorul optional de joi?",
             "Am gasit un articol interesant, il pun in resurse.",
             "Intrebare rapida: care e diferenta intre X si Y?",
             "Am terminat tema, daca vrea cineva sa faca code review.",
             "Meeting online duminica la 18:00, link in urmatorul mesaj."];

        private static readonly string[] Emojis =
            ["thumbs_up","heart","laugh","party","thinking","eyes","fire","check","hundred","wow","pray","muscle","target","rocket","star"];

        private static readonly string[] AuditActions =
            ["USER_CREATED","POST_DELETED","USER_BANNED","ROLE_CHANGED",
             "REPORT_RESOLVED","CONTENT_FLAGGED","GROUP_CREATED","MEMBER_KICKED",
             "POST_PINNED","USER_VERIFIED","COMMUNITY_CREATED","CHANNEL_DELETED"];

        public const string DefaultPassword = "Test@1234!";

        // ─── ENTRY POINT ──────────────────────────────────────────────────────────

        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine("[LoadSeeder] Starting ENTERPRISE seed...");

            try
            {
                await SeedRanksAsync(context);
                var tags        = await SeedTagsAsync(context);
                var users       = await SeedUsersAsync(context);
                await SeedVerificationRequestsAsync(context, users);
                var files       = await SeedStoredFilesAsync(context, users);
                await SeedFriendshipsAsync(context, users);
                var communities = await SeedCommunitiesAsync(context, users, tags);
                var groups      = await SeedStudyGroupsAsync(context, users);
                await SeedGroupRolesAndMembershipsAsync(context, groups, users);
                await SeedChannelCategoriesAsync(context, groups);
                var channels    = await SeedChannelsAsync(context, groups);
                var posts       = await SeedPostsAsync(context, communities, channels, users, tags);
                var messages    = await SeedMessagesAsync(context, posts, channels, users);
                await SeedPostVotesAsync(context, posts, users);
                await SeedPostViewsAsync(context, posts, users);
                await SeedMessageReactionsAsync(context, messages, users);
                await SeedDirectMessagingAsync(context, users);
                await SeedAuditLogsAsync(context, users);
                await SeedReportsAsync(context, users, posts);
                await SeedNotificationsAsync(context, users);
                await SeedLearningResourcesAsync(context, users, groups, files);
                await SeedEventsAsync(context, communities, users);
                await SeedGroupInvitesAsync(context, groups);

                sw.Stop();
                Console.WriteLine($"\n[LoadSeeder] Done in {sw.Elapsed.TotalSeconds:F1}s");
                PrintSummary();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[LoadSeeder] FATAL ERROR:");
                for (var e = ex; e != null; e = e.InnerException)
                    Console.WriteLine($"  [{e.GetType().Name}] {e.Message}");
                throw;
            }
        }

        // ─── BULK INSERT ──────────────────────────────────────────────────────────

        private static async Task BulkInsertAsync<T>(
            ApplicationDbContext ctx,
            IReadOnlyList<T> entities,
            string tableName,
            string label,
            Func<T, string>? keySelector = null) where T : class
        {
            if (entities.Count == 0) return;

            var sw = Stopwatch.StartNew();

            // Only map scalar/value-type columns — skip navigation properties
            var props = typeof(T).GetProperties()
                .Where(p => p.GetMethod != null && p.SetMethod != null)
                .Where(p => p.PropertyType.IsValueType
                         || p.PropertyType == typeof(string)
                         || p.PropertyType == typeof(Guid)
                         || p.PropertyType == typeof(Guid?))
                .ToArray();

            // Deduplicate in-memory before sending
            IReadOnlyList<T> data = entities;
            if (keySelector != null)
            {
                var seen = new HashSet<string>();
                data = entities.Where(e => seen.Add(keySelector(e))).ToList();
                if (data.Count < entities.Count)
                    Console.WriteLine($"[LoadSeeder]   {label}: removed {entities.Count - data.Count:N0} dupes");
            }

            var conn = ctx.Database.GetDbConnection() as SqlConnection
                ?? throw new NotSupportedException("SQL Server required.");

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (conn.State != ConnectionState.Open) await conn.OpenAsync();

                    using var bc = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, null)
                    {
                        DestinationTableName = tableName,
                        BatchSize            = Cfg.BatchSize,
                        BulkCopyTimeout      = 600,
                        EnableStreaming       = true
                    };
                    foreach (var p in props) bc.ColumnMappings.Add(p.Name, p.Name);

                    using var reader = new EntityDataReader<T>(data, props);
                    await bc.WriteToServerAsync(reader);

                    Console.WriteLine($"[LoadSeeder]   {label}: {data.Count:N0} rows in {sw.Elapsed.TotalSeconds:F2}s");
                    return;
                }
                catch (SqlException ex) when (ex.Number == 2627 && attempt < 2)
                {
                    Console.WriteLine($"[LoadSeeder]   {label}: dup key attempt {attempt + 1}, retrying...");
                    await Task.Delay(2000);
                }
                catch (Exception ex) when (attempt < 2)
                {
                    Console.WriteLine($"[LoadSeeder]   {label}: {ex.Message[..Math.Min(80,ex.Message.Length)]}, retry {attempt + 1}...");
                    await Task.Delay(3000);
                }
            }

            // EF fallback
            Console.WriteLine($"[LoadSeeder]   {label}: EF fallback...");
            int bs = Cfg.BatchSize / 4;
            for (int i = 0; i < data.Count; i += bs)
            {
                var batch = data.Skip(i).Take(bs).ToList();
                await ctx.Set<T>().AddRangeAsync(batch);
                try { await ctx.SaveChangesAsync(); } catch { }
                ctx.ChangeTracker.Clear();
                Console.Write($"\r[LoadSeeder]   {label}: {Math.Min(i + bs, data.Count):N0}/{data.Count:N0}");
            }
            Console.WriteLine();
        }

        // ─── IDataReader ──────────────────────────────────────────────────────────

        private sealed class EntityDataReader<T> : IDataReader where T : class
        {
            private readonly IReadOnlyList<T> _data;
            private readonly System.Reflection.PropertyInfo[] _props;
            private int _pos = -1;

            public EntityDataReader(IReadOnlyList<T> data, System.Reflection.PropertyInfo[] props)
            { _data = data; _props = props; }

            public int  FieldCount      => _props.Length;
            public bool IsClosed        => false;
            public int  Depth           => 0;
            public int  RecordsAffected => -1;

            public bool      Read()              => ++_pos < _data.Count;
            public bool      NextResult()        => false;
            public void      Close()             { }
            public void      Dispose()           { }
            public DataTable GetSchemaTable()    => null!;

            public object GetValue(int i)        => _props[i].GetValue(_data[_pos]) ?? DBNull.Value;
            public bool   IsDBNull(int i)        => GetValue(i) is DBNull;
            public int    GetOrdinal(string n)   => Array.FindIndex(_props, p => p.Name == n);
            public string GetName(int i)         => _props[i].Name;
            public Type   GetFieldType(int i)    => _props[i].PropertyType;
            public string GetDataTypeName(int i) => GetFieldType(i).Name;

            public object this[int i]    => GetValue(i);
            public object this[string n] => GetValue(GetOrdinal(n));

            public bool     GetBoolean(int i)  => (bool)GetValue(i);
            public byte     GetByte(int i)     => (byte)GetValue(i);
            public char     GetChar(int i)     => (char)GetValue(i);
            public DateTime GetDateTime(int i) => (DateTime)GetValue(i);
            public decimal  GetDecimal(int i)  => (decimal)GetValue(i);
            public double   GetDouble(int i)   => (double)GetValue(i);
            public float    GetFloat(int i)    => (float)GetValue(i);
            public Guid     GetGuid(int i)     => (Guid)GetValue(i);
            public short    GetInt16(int i)    => (short)GetValue(i);
            public int      GetInt32(int i)    => (int)GetValue(i);
            public long     GetInt64(int i)    => (long)GetValue(i);
            public string   GetString(int i)   => (string)GetValue(i);

            public int GetValues(object[] v)
            { for (int i = 0; i < _props.Length; i++) v[i] = GetValue(i); return _props.Length; }

            public long        GetBytes(int i, long fo, byte[]? b, int bo, int l) => throw new NotImplementedException();
            public long        GetChars(int i, long fo, char[]? b, int bo, int l) => throw new NotImplementedException();
            public IDataReader GetData(int i)                                      => throw new NotImplementedException();
        }

        // ─── HELPERS ──────────────────────────────────────────────────────────────

        // FIX: IReadOnlyList so both arrays and List<T> work without cast errors
        private static T Pick<T>(IReadOnlyList<T> list) => list[Rng.Next(list.Count)];
        private static T Pick<T>(T[] arr)               => arr[Rng.Next(arr.Length)];

        private static List<T> PickMany<T>(IReadOnlyList<T> list, int n)
        {
            var copy   = list.ToList();
            var result = new List<T>(n);
            for (int i = 0; i < n && copy.Count > 0; i++)
            {
                int idx = Rng.Next(copy.Count);
                result.Add(copy[idx]);
                copy.RemoveAt(idx);
            }
            return result;
        }

        // FIX: use PasswordHasher so login works after seed
        private static string HashPassword(string password)
            => new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), password);

        // ─── SEED METHODS ─────────────────────────────────────────────────────────

        private static async Task<List<UserRank>> SeedRanksAsync(ApplicationDbContext ctx)
        {
            if (await ctx.UserRanks.AnyAsync())
                return await ctx.UserRanks.AsNoTracking().ToListAsync();

            var ranks = new List<UserRank>
            {
                new() { Name = "Novice",     MinPoints = 0,      IconUrl = "/icons/ranks/novice.png"     },
                new() { Name = "Apprentice", MinPoints = 500,    IconUrl = "/icons/ranks/apprentice.png" },
                new() { Name = "Scholar",    MinPoints = 1_500,  IconUrl = "/icons/ranks/scholar.png"    },
                new() { Name = "Expert",     MinPoints = 3_500,  IconUrl = "/icons/ranks/expert.png"     },
                new() { Name = "Master",     MinPoints = 7_500,  IconUrl = "/icons/ranks/master.png"     },
                new() { Name = "Legend",     MinPoints = 15_000, IconUrl = "/icons/ranks/legend.png"     },
            };
            await ctx.UserRanks.AddRangeAsync(ranks);
            await ctx.SaveChangesAsync();
            Console.WriteLine($"[LoadSeeder]   Ranks: {ranks.Count} seeded.");
            return ranks;
        }

        private static async Task<List<Tag>> SeedTagsAsync(ApplicationDbContext ctx)
        {
            if (await ctx.Tags.AnyAsync())
                return await ctx.Tags.AsNoTracking().ToListAsync();

            var names = new[]
            {
                "Programming","C#","SQL","Machine Learning","Economics","Statistics",
                "Web Development","Database","AI","Cloud","Security","Finance",
                "Management","Marketing","Research","Algorithms","Data Structures",
                "DevOps","Mobile","UI/UX","Networking","Operating Systems",
                "Python","Java","Docker","Kubernetes","React","Angular","Vue",
                "TypeScript","Rust","Go","Microservices","Testing","Architecture"
            };
            var tags = names.Select(n => new Tag { Name = n }).ToList();
            await ctx.Tags.AddRangeAsync(tags);
            await ctx.SaveChangesAsync();
            Console.WriteLine($"[LoadSeeder]   Tags: {tags.Count} seeded.");
            return tags;
        }

        private static async Task<List<ApplicationUser>> SeedUsersAsync(ApplicationDbContext ctx)
        {
            if (await ctx.Users.AnyAsync())
            {
                Console.WriteLine("[LoadSeeder]   Users: already seeded, loading...");
                return await ctx.Users.AsNoTracking().ToListAsync();
            }

            var sw   = Stopwatch.StartNew();
            var hash = HashPassword(DefaultPassword);
            var repLevels = new[] { 50,300,800,1200,2500,5000,10000,18000 };

            var bag   = new ConcurrentBag<ApplicationUser>();
            int total = Math.Min(Cfg.UserCount, 100_000);
            int cpus  = Environment.ProcessorCount;
            int chunk = (int)Math.Ceiling((double)total / cpus);

            // FIX: Task.Run instead of Parallel.ForEachAsync+Partitioner (avoids type inference CS0411)
            await Task.WhenAll(Enumerable.Range(0, cpus).Select(part => Task.Run(() =>
            {
                int from = part * chunk;
                int to   = Math.Min(from + chunk, total);
                for (int i = from; i < to; i++)
                {
                    var first = Pick(FirstNames);
                    var last  = Pick(LastNames);
                    var email = $"{first.ToLower()}.{last.ToLower()}{i}{Pick(Domains)}";

                    double r = Rng.NextDouble();
                    var role = r < 0.02 ? Role.Admin : r < 0.07 ? Role.Moderator : Role.Member;
                    var rep  = Pick(repLevels) + Rng.Next(0, 400);
                    var verified = role != Role.Member || (rep > 1000 && Rng.NextDouble() < 0.7);

                    bag.Add(new ApplicationUser
                    {
                        Id                 = Guid.NewGuid().ToString(),
                        UserName           = email,
                        Email              = email,
                        NormalizedEmail    = email.ToUpperInvariant(),
                        NormalizedUserName = email.ToUpperInvariant(),
                        FullName           = $"{first} {last}",
                        EmailConfirmed     = true,
                        Role               = role,
                        IsVerified         = verified,
                        ReputationPoints   = rep,
                        Bio                = $"{first} {last} este {Pick(Occupations)} la {Pick(Institutions)}.",
                        Institution        = Pick(Institutions),
                        Occupation         = Pick(Occupations),
                        Interests          = string.Join(", ", PickMany(Interests, Rng.Next(2, 6))),
                        SocialLinks        = "{}",
                        AvatarUrl          = $"https://randomuser.me/api/portraits/{(Rng.Next(2) == 0 ? "men" : "women")}/{Rng.Next(1, 99)}.jpg",
                        Status             = Pick(new[] { "Online","Offline","Away" }),
                        LastActive         = DateTime.UtcNow.AddDays(-Rng.Next(1, 180)),
                        SecurityStamp      = Guid.NewGuid().ToString(),
                        CreatedAt          = DateTime.UtcNow.AddMonths(-Rng.Next(1, 36)),
                        PasswordHash       = hash
                    });
                }
            })));

            var list = bag.ToList();
            await BulkInsertAsync(ctx, list, "Users", "Users", u => u.Email ?? u.Id);
            Console.WriteLine($"[LoadSeeder]   Users total: {list.Count:N0} in {sw.Elapsed.TotalSeconds:F1}s");
            return list;
        }

        private static async Task SeedVerificationRequestsAsync(
            ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            if (await ctx.VerificationRequests.AnyAsync()) return;

            var admins = users.Where(u => u.Role == Role.Admin || u.Role == Role.Moderator).Take(10).ToList();
            var reqs   = new List<VerificationRequest>();

            foreach (var user in users.Where(u => u.IsVerified).Take(5000))
            {
                var submitted = DateTime.UtcNow.AddDays(-Rng.Next(5, 180));
                reqs.Add(new VerificationRequest
                {
                    Id           = Guid.NewGuid(),
                    UserId       = user.Id,
                    StudentIdUrl = $"/uploads/verification/id_{Guid.NewGuid():N}.jpg",
                    CarnetUrl    = $"/uploads/verification/carnet_{Guid.NewGuid():N}.jpg",
                    Status       = Status.Approved,
                    ProcessedBy  = admins.Any() ? Pick(admins).Id : user.Id,
                    ProcessedAt  = submitted.AddDays(Rng.Next(1, 10)),
                    SubmittedAt  = submitted
                });
            }
            await BulkInsertAsync(ctx, reqs, "VerificationRequests", "VerificationRequests");
        }

        private static async Task<List<StoredFile>> SeedStoredFilesAsync(
            ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            if (await ctx.StoredFiles.AnyAsync())
                return await ctx.StoredFiles.AsNoTracking().ToListAsync();

            var exts  = new[] { "pdf","docx","xlsx","pptx","zip","png","jpg","mp4","csv" };
            var files = new List<StoredFile>();

            for (int i = 0; i < Math.Min(8000, users.Count / 5); i++)
            {
                var ext = Pick(exts);
                files.Add(new StoredFile
                {
                    Id         = Guid.NewGuid(),
                    FileName   = $"document_{i:D6}.{ext}",
                    FilePath   = $"/uploads/files/{Guid.NewGuid():N}.{ext}",
                    FileType   = $"application/{ext}",
                    FileSize   = Rng.Next(512, 50 * 1024 * 1024),
                    UploaderId = Pick(users).Id,
                    UploadedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 365))
                });
            }
            await BulkInsertAsync(ctx, files, "StoredFiles", "StoredFiles");
            return files;
        }

        private static async Task SeedFriendshipsAsync(
            ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            if (await ctx.Friendships.AnyAsync()) return;

            var friendships = new List<Friendship>();
            var seen        = new HashSet<string>();
            var sample      = users.Take(Math.Min(15_000, users.Count)).ToList();

            foreach (var user in sample)
            {
                int degree = Rng.Next(3, 20);
                foreach (var other in sample.OrderBy(_ => Rng.Next()).Take(degree))
                {
                    if (other.Id == user.Id) continue;
                    var key = string.Compare(user.Id, other.Id, StringComparison.Ordinal) < 0
                        ? $"{user.Id}|{other.Id}" : $"{other.Id}|{user.Id}";
                    if (!seen.Add(key)) continue;

                    friendships.Add(new Friendship
                    {
                        RequesterId = user.Id,
                        AddresseeId = other.Id,
                        Status      = Rng.NextDouble() < 0.75 ? FriendshipStatus.Accepted : FriendshipStatus.Pending,
                        CreatedAt   = DateTime.UtcNow.AddDays(-Rng.Next(1, 365))
                    });
                }
            }
            await BulkInsertAsync(ctx, friendships, "Friendships", "Friendships",
                f => string.Compare(f.RequesterId, f.AddresseeId, StringComparison.Ordinal) < 0
                    ? $"{f.RequesterId}|{f.AddresseeId}"
                    : $"{f.AddresseeId}|{f.RequesterId}");
        }

        private static async Task<List<Community>> SeedCommunitiesAsync(
            ApplicationDbContext ctx, List<ApplicationUser> users, List<Tag> tags)
        {
            if (await ctx.Communities.AnyAsync())
                return await ctx.Communities.AsNoTracking().ToListAsync();

            var defs = new[]
            {
                ("Informatica Economica",    "ineconomica",   "Comunitate dedicata studentilor de la Informatica Economica"),
                ("Cibernetica si Statistica","cibernetica",   "Matematica aplicata, statistica si modelare economica"),
                ("Inginerie Software",       "ing-software",  "Arhitecturi, design patterns, best practices in software"),
                ("Machine Learning & AI",    "ml-ai",         "Invatare automata, retele neuronale si inteligenta artificiala"),
                ("Securitate Informatica",   "securitate",    "Cybersecurity, penetration testing, CTF competitions"),
                ("Web & Mobile Development", "web-mobile",    "Frontend, backend, aplicatii mobile native si cross-platform"),
                ("Data Science",             "data-science",  "Analiza de date, vizualizare, business intelligence"),
                ("Cloud & DevOps",           "cloud-devops",  "AWS, Azure, GCP, Kubernetes, CI/CD pipelines"),
                ("Competitive Programming",  "competitive",   "Algoritmi, structuri de date, concursuri de programare"),
                ("Open Source Romania",      "open-source",   "Proiecte open source, contributii, comunitate"),
            };

            var staff       = users.Where(u => u.Role != Role.Member).Take(20).ToList();
            var communities = new List<Community>();

            foreach (var (name, slug, desc) in defs)
            {
                communities.Add(new Community
                {
                    Id          = Guid.NewGuid(),
                    Name        = name,
                    Slug        = slug,
                    Description = desc,
                    CreatorId   = Pick(staff).Id,
                    CreatedAt   = DateTime.UtcNow.AddMonths(-Rng.Next(3, 36))
                });
            }
            await ctx.Communities.AddRangeAsync(communities);
            await ctx.SaveChangesAsync();

            var memberships = new List<CommunityMembership>();
            var memberSet   = new HashSet<string>();

            foreach (var user in users.Take(Math.Min(80_000, users.Count)))
            {
                int joins = Rng.Next(1, 6);
                foreach (var comm in communities.OrderBy(_ => Rng.Next()).Take(joins))
                {
                    var key = $"{comm.Id}|{user.Id}";
                    if (!memberSet.Add(key)) continue;
                    memberships.Add(new CommunityMembership
                    {
                        CommunityId = comm.Id,
                        UserId      = user.Id,
                        Role        = staff.Any(s => s.Id == user.Id) ? CommunityRole.Moderator : CommunityRole.Member,
                        JoinedAt    = DateTime.UtcNow.AddDays(-Rng.Next(1, 365))
                    });
                }
            }
            await BulkInsertAsync(ctx, memberships, "CommunityMemberships", "CommunityMemberships",
                m => $"{m.CommunityId}|{m.UserId}");
            Console.WriteLine($"[LoadSeeder]   Communities: {communities.Count} | Memberships: {memberships.Count:N0}");
            return communities;
        }

        private static async Task<List<StudyGroup>> SeedStudyGroupsAsync(
            ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            if (await ctx.StudyGroups.AnyAsync())
                return await ctx.StudyGroups.AsNoTracking().ToListAsync();

            var defs = new[]
            {
                ("Pregatire Licenta 2025",    "Pregatire intensiva pentru examenul de licenta si disertatie"),
                ("DotNet Advanced",           "Concepte avansate de C# si ecosistemul .NET 8/9"),
                ("C/C++ Algorithms",          "Competitive programming si algoritmi clasici in C++"),
                ("SQL Mastery",               "Optimizare, indexare si tuning pentru PostgreSQL si SQL Server"),
                ("DevOps & Cloud Native",     "Docker, Kubernetes, CI/CD, AWS, Azure, Terraform"),
                ("React & TypeScript",        "Frontend modern cu React 18, TypeScript si Next.js"),
                ("Python & Data Science",     "NumPy, Pandas, Scikit-learn, TensorFlow, PyTorch, Jupyter"),
                ("Mobile Dev - Flutter",      "Cross-platform mobile cu Flutter, Dart si Firebase"),
                ("Cybersecurity Basics",      "Introducere in securitate, CTF-uri si ethical hacking"),
                ("System Design",             "Proiectarea sistemelor scalabile, microservices, caching"),
                ("Open Source Contributors",  "Contributii la proiecte open source, code review si PR-uri"),
                ("Game Development Unity",    "Dezvoltare jocuri 2D/3D cu Unity si C#"),
            };

            var staff  = users.Where(u => u.Role != Role.Member).Take(20).ToList();
            var groups = new List<StudyGroup>();

            foreach (var (name, desc) in defs)
            {
                groups.Add(new StudyGroup
                {
                    Id          = Guid.NewGuid(),
                    Name        = name,
                    Description = desc,
                    OwnerId     = Pick(staff).Id,
                    IsPublic    = Rng.NextDouble() > 0.25,
                    SubjectArea = Pick(Interests),
                    InviteCode  = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                    CreatedAt   = DateTime.UtcNow.AddMonths(-Rng.Next(1, 24))
                });
            }
            await ctx.StudyGroups.AddRangeAsync(groups);
            await ctx.SaveChangesAsync();
            Console.WriteLine($"[LoadSeeder]   StudyGroups: {groups.Count} seeded.");
            return groups;
        }

        private static async Task SeedGroupRolesAndMembershipsAsync(
            ApplicationDbContext ctx, List<StudyGroup> groups, List<ApplicationUser> users)
        {
            if (await ctx.GroupRoles.AnyAsync()) return;

            var allRoles = new List<GroupRole>();
            foreach (var g in groups)
            {
                allRoles.Add(new GroupRole { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Admin",     Permissions = "ALL",                 Priority = 100 });
                allRoles.Add(new GroupRole { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Moderator", Permissions = "MANAGE_MESSAGES,KICK",Priority = 50  });
                allRoles.Add(new GroupRole { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Member",    Permissions = "READ,WRITE",          Priority = 10  });
                allRoles.Add(new GroupRole { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Guest",     Permissions = "READ",                Priority = 1   });
            }
            await ctx.GroupRoles.AddRangeAsync(allRoles);
            await ctx.SaveChangesAsync();

            var adminRoles  = allRoles.Where(r => r.Name == "Admin").ToList();
            var memberRoles = allRoles.Where(r => r.Name == "Member").ToList();
            var memberships = new List<GroupMembership>();
            var memSet      = new HashSet<string>();

            foreach (var g in groups)
            {
                var adminRole  = adminRoles.First(r => r.GroupId == g.Id);
                var memberRole = memberRoles.First(r => r.GroupId == g.Id);

                if (g.OwnerId != null && memSet.Add($"{g.Id}|{g.OwnerId}"))
                    memberships.Add(new GroupMembership { GroupId = g.Id, UserId = g.OwnerId, GroupRoleId = adminRole.Id, JoinedAt = DateTime.UtcNow });

                int memberCount = Rng.Next(200, 600);
                foreach (var u in users.Where(u => u.Id != g.OwnerId).OrderBy(_ => Rng.Next()).Take(memberCount))
                {
                    if (!memSet.Add($"{g.Id}|{u.Id}")) continue;
                    memberships.Add(new GroupMembership
                    {
                        GroupId     = g.Id,
                        UserId      = u.Id,
                        GroupRoleId = memberRole.Id,
                        JoinedAt    = DateTime.UtcNow.AddDays(-Rng.Next(1, 365))
                    });
                }
            }
            await BulkInsertAsync(ctx, memberships, "GroupMemberships", "GroupMemberships",
                m => $"{m.GroupId}|{m.UserId}");
        }

        private static async Task SeedChannelCategoriesAsync(
            ApplicationDbContext ctx, List<StudyGroup> groups)
        {
            if (await ctx.ChannelCategories.AnyAsync()) return;

            var cats = new List<ChannelCategory>();
            foreach (var g in groups)
            {
                cats.Add(new ChannelCategory { Id = Guid.NewGuid(), GroupId = g.Id, Name = "General",   Position = 0 });
                cats.Add(new ChannelCategory { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Resurse",   Position = 1 });
                cats.Add(new ChannelCategory { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Proiecte",  Position = 2 });
                cats.Add(new ChannelCategory { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Off-Topic", Position = 3 });
            }
            await BulkInsertAsync(ctx, cats, "ChannelCategories", "ChannelCategories");
        }

        private static async Task<List<Channel>> SeedChannelsAsync(
            ApplicationDbContext ctx, List<StudyGroup> groups)
        {
            if (await ctx.Channels.AnyAsync())
                return await ctx.Channels.AsNoTracking().ToListAsync();

            var categories = await ctx.ChannelCategories.AsNoTracking().ToListAsync();
            var channels   = new List<Channel>();

            foreach (var g in groups)
            {
                Guid? catGen  = categories.FirstOrDefault(c => c.GroupId == g.Id && c.Name == "General")?.Id;
                Guid? catRes  = categories.FirstOrDefault(c => c.GroupId == g.Id && c.Name == "Resurse")?.Id;
                Guid? catProj = categories.FirstOrDefault(c => c.GroupId == g.Id && c.Name == "Proiecte")?.Id;
                Guid? catOff  = categories.FirstOrDefault(c => c.GroupId == g.Id && c.Name == "Off-Topic")?.Id;

                channels.AddRange(new[]
                {
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catGen,  Name = "general",        Type = ChannelType.Text,  Topic = "Discutii generale"           },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catGen,  Name = "anunturi",       Type = ChannelType.Text,  Topic = "Anunturi importante"          },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catOff,  Name = "off-topic",      Type = ChannelType.Text,  Topic = "Subiecte neoficiale"          },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catOff,  Name = "fun-memes",      Type = ChannelType.Text,  Topic = "Meme-uri si glume"            },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catRes,  Name = "resurse",        Type = ChannelType.Text,  Topic = "Materiale si link-uri"        },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catRes,  Name = "carti-cursuri",  Type = ChannelType.Text,  Topic = "Carti si cursuri recomandate" },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catProj, Name = "proiecte",       Type = ChannelType.Text,  Topic = "Colaborare proiecte"          },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catProj, Name = "code-review",    Type = ChannelType.Text,  Topic = "Review cod si PR-uri"         },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catGen,  Name = "Voice General",  Type = ChannelType.Voice                                         },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catProj, Name = "Voice Proiecte", Type = ChannelType.Voice                                         },
                });
            }
            await BulkInsertAsync(ctx, channels, "Channels", "Channels");
            Console.WriteLine($"[LoadSeeder]   Channels: {channels.Count} seeded.");
            return channels;
        }

        private static async Task<List<Post>> SeedPostsAsync(
            ApplicationDbContext  ctx,
            List<Community>       communities,
            List<Channel>         channels,
            List<ApplicationUser> users,
            List<Tag>             tags)
        {
            if (await ctx.Posts.AnyAsync())
            {
                Console.WriteLine("[LoadSeeder]   Posts: already seeded, loading...");
                return await ctx.Posts.AsNoTracking().Take(10_000).ToListAsync();
            }

            var posts    = new List<Post>();
            var postTags = new List<PostTag>();
            var tagSeen  = new HashSet<string>();

            foreach (var comm in communities)
            {
                for (int i = 0; i < Cfg.PostsPerCommunity; i++)
                {
                    var createdAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 730));
                    var post = new Post
                    {
                        Id               = Guid.NewGuid(),
                        CommunityId      = comm.Id,
                        AuthorId         = Pick(users).Id,
                        Title            = Pick(PostTitles),
                        Content          = Pick(PostContents) + $"\n\n[Post #{i + 1} din {comm.Name}]",
                        CreatedAt        = createdAt,
                        IsSolved         = Rng.NextDouble() > 0.6,
                        IsPinned         = Rng.NextDouble() > 0.95,
                        IsLocked         = Rng.NextDouble() > 0.97,
                        ModerationStatus = ModerationStatus.Approved,
                    };
                    posts.Add(post);
                    // FIX: PostTag without navigation properties (Post? Tag? are nullable in entity)
                    foreach (var tag in PickMany(tags, Rng.Next(1, 4)))
                    {
                        var tk = $"{post.Id}|{tag.Id}";
                        if (tagSeen.Add(tk))
                            postTags.Add(new PostTag { PostId = post.Id, TagId = tag.Id });
                    }
                }
            }

            foreach (var chan in channels.Where(c => c.Type == ChannelType.Text))
            {
                for (int i = 0; i < Cfg.PostsPerChannel; i++)
                {
                    posts.Add(new Post
                    {
                        Id               = Guid.NewGuid(),
                        ChannelId        = chan.Id,
                        AuthorId         = Pick(users).Id,
                        Title            = Pick(PostTitles),
                        Content          = Pick(PostContents),
                        CreatedAt        = DateTime.UtcNow.AddDays(-Rng.Next(1, 365)),
                        IsSolved         = false,
                        ModerationStatus = ModerationStatus.Approved,
                    });
                }
            }

            await BulkInsertAsync(ctx, posts,    "Posts",    "Posts");
            await BulkInsertAsync(ctx, postTags, "PostTags", "PostTags", pt => $"{pt.PostId}|{pt.TagId}");
            Console.WriteLine($"[LoadSeeder]   Posts: {posts.Count:N0} | PostTags: {postTags.Count:N0}");
            return posts;
        }

        private static async Task<List<Message>> SeedMessagesAsync(
            ApplicationDbContext  ctx,
            List<Post>            posts,
            List<Channel>         channels,
            List<ApplicationUser> users)
        {
            if (await ctx.Messages.AnyAsync())
            {
                Console.WriteLine("[LoadSeeder]   Messages: already seeded, loading...");
                return await ctx.Messages.AsNoTracking().Take(10_000).ToListAsync();
            }

            var messages = new List<Message>();

            foreach (var post in posts.Where(p => p.CommunityId != null).Take(5000))
            {
                int count = Rng.Next(0, Cfg.CommentsPerPost + 1);
                for (int i = 0; i < count; i++)
                {
                    messages.Add(new Message
                    {
                        Id               = Guid.NewGuid(),
                        PostId           = post.Id,
                        AuthorId         = Pick(users).Id,
                        Content          = Pick(CommentContents),
                        CreatedAt        = post.CreatedAt.AddMinutes(Rng.Next(1, 20_000)),
                        ModerationStatus = ModerationStatus.Approved,
                    });
                }
            }

            foreach (var chan in channels.Where(c => c.Type == ChannelType.Text))
            {
                int count = Rng.Next(20, Cfg.ChannelMessagesPerChannel + 1);
                for (int i = 0; i < count; i++)
                {
                    messages.Add(new Message
                    {
                        Id               = Guid.NewGuid(),
                        ChannelId        = chan.Id,
                        AuthorId         = Pick(users).Id,
                        Content          = Pick(ChannelMsgContents),
                        CreatedAt        = DateTime.UtcNow.AddHours(-Rng.Next(1, 17_520)),
                        ModerationStatus = ModerationStatus.Approved,
                    });
                }
            }

            await BulkInsertAsync(ctx, messages, "Messages", "Messages");
            Console.WriteLine($"[LoadSeeder]   Messages: {messages.Count:N0}");
            return messages;
        }

        private static async Task SeedPostVotesAsync(
            ApplicationDbContext ctx, List<Post> posts, List<ApplicationUser> users)
        {
            if (await ctx.PostVotes.AnyAsync()) { Console.WriteLine("[LoadSeeder]   PostVotes: already seeded."); return; }

            var votes = new List<PostVote>();
            var seen  = new HashSet<string>();

            foreach (var post in posts.Take(5000))
            {
                int voters = Rng.Next(0, Math.Min(Cfg.MaxVotersPerPost, 300));
                for (int i = 0; i < voters; i++)
                {
                    var user = Pick(users);
                    var key  = $"{post.Id}|{user.Id}";
                    if (!seen.Add(key)) continue;
                    votes.Add(new PostVote
                    {
                        PostId    = post.Id,
                        UserId    = user.Id,
                        VoteValue = (short)(Rng.NextDouble() > 0.15 ? 1 : -1)
                    });
                }
            }
            // FIX: key selector uses direct properties, no cast needed
            await BulkInsertAsync(ctx, votes, "PostVotes", "PostVotes", v => $"{v.PostId}|{v.UserId}");
        }

        private static async Task SeedPostViewsAsync(
            ApplicationDbContext ctx, List<Post> posts, List<ApplicationUser> users)
        {
            if (await ctx.PostViews.AnyAsync()) { Console.WriteLine("[LoadSeeder]   PostViews: already seeded."); return; }

            var views = new List<PostView>();
            var seen  = new HashSet<string>();

            foreach (var post in posts.Take(5000))
            {
                int viewers = Rng.Next(5, Math.Min(Cfg.MaxViewersPerPost, 400));
                for (int i = 0; i < viewers; i++)
                {
                    var user = Pick(users);
                    var key  = $"{post.Id}|{user.Id}";
                    if (!seen.Add(key)) continue;
                    views.Add(new PostView
                    {
                        Id       = Guid.NewGuid(),      // PostView has its own Guid PK
                        PostId   = post.Id,
                        UserId   = user.Id,
                        ViewedAt = post.CreatedAt.AddMinutes(Rng.Next(1, 30_000))
                    });
                }
            }
            await BulkInsertAsync(ctx, views, "PostViews", "PostViews", v => $"{v.PostId}|{v.UserId}");
        }

        private static async Task SeedMessageReactionsAsync(
            ApplicationDbContext ctx, List<Message> messages, List<ApplicationUser> users)
        {
            if (await ctx.MessageReactions.AnyAsync()) { Console.WriteLine("[LoadSeeder]   MessageReactions: already seeded."); return; }

            var reactions = new List<MessageReaction>();
            var seen      = new HashSet<string>();

            foreach (var msg in messages.Take(8000))
            {
                int count = Rng.Next(0, 10);
                for (int i = 0; i < count; i++)
                {
                    var user  = Pick(users);
                    // FIX: use ASCII emoji codes instead of actual emoji chars to avoid SqlBulkCopy encoding issues
                    var emoji = Pick(Emojis);
                    var key   = $"{msg.Id}|{user.Id}|{emoji}";
                    if (!seen.Add(key)) continue;
                    reactions.Add(new MessageReaction
                    {
                        MessageId = msg.Id,
                        UserId    = user.Id,
                        EmojiCode = emoji
                    });
                }
            }

            // FIX: Use EF batches for reactions — avoids SqlBulkCopy string encoding issues with emoji
            Console.WriteLine($"[LoadSeeder]   MessageReactions: inserting {reactions.Count:N0} rows...");
            var sw = Stopwatch.StartNew();
            const int bs = 3000;
            for (int i = 0; i < reactions.Count; i += bs)
            {
                var batch = reactions.Skip(i).Take(bs).ToList();
                await ctx.MessageReactions.AddRangeAsync(batch);
                try { await ctx.SaveChangesAsync(); }
                catch { ctx.ChangeTracker.Clear(); }
                ctx.ChangeTracker.Clear();
                Console.Write($"\r[LoadSeeder]   MessageReactions: {Math.Min(i + bs, reactions.Count):N0}/{reactions.Count:N0}");
            }
            Console.WriteLine($"\n[LoadSeeder]   MessageReactions: done in {sw.Elapsed.TotalSeconds:F2}s");
        }

        private static async Task SeedDirectMessagingAsync(
            ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            if (await ctx.DirectConversations.AnyAsync()) { Console.WriteLine("[LoadSeeder]   DirectConversations: already seeded."); return; }

            var seen          = new HashSet<string>();
            var conversations = new List<DirectConversation>();
            var participants  = new List<DirectConversationParticipant>();

            for (int i = 0; i < Math.Min(Cfg.DirectConvCount, 5000); i++)
            {
                var u1 = Pick(users);
                var u2 = Pick(users);
                if (u1.Id == u2.Id) continue;
                var key = string.Compare(u1.Id, u2.Id, StringComparison.Ordinal) < 0
                    ? $"{u1.Id}|{u2.Id}" : $"{u2.Id}|{u1.Id}";
                if (!seen.Add(key)) continue;

                var cid = Guid.NewGuid();
                conversations.Add(new DirectConversation { Id = cid, CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 365)) });
                participants.Add(new DirectConversationParticipant { ConversationId = cid, UserId = u1.Id });
                participants.Add(new DirectConversationParticipant { ConversationId = cid, UserId = u2.Id });
            }

            await BulkInsertAsync(ctx, conversations, "DirectConversations",            "DirectConversations");
            await BulkInsertAsync(ctx, participants,  "DirectConversationParticipants", "DirectConversationParticipants");
            Console.WriteLine($"[LoadSeeder]   DirectConversations: {conversations.Count:N0}");
        }

        private static async Task SeedAuditLogsAsync(
            ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            if (await ctx.AuditLogs.AnyAsync()) { Console.WriteLine("[LoadSeeder]   AuditLogs: already seeded."); return; }

            var admins   = users.Where(u => u.Role == Role.Admin || u.Role == Role.Moderator).Take(100).ToList();
            var entities = new[] { "User","Post","Message","Community","Group","Channel","Report" };
            var logs     = new List<AuditLog>();

            for (int i = 0; i < Math.Min(Cfg.AuditLogCount, 5000); i++)
            {
                logs.Add(new AuditLog
                {
                    Id           = Guid.NewGuid(),
                    ActorId      = admins.Any() ? Pick(admins).Id : Pick(users).Id,
                    ActionType   = Pick(AuditActions),
                    TargetEntity = Pick(entities),
                    TargetId     = Guid.NewGuid(),
                    Changes      = $"{{\"auto\":true,\"index\":{i}}}",
                    CreatedAt    = DateTime.UtcNow.AddDays(-Rng.Next(1, 365))
                });
            }
            await BulkInsertAsync(ctx, logs, "AuditLogs", "AuditLogs");
        }

        private static async Task SeedReportsAsync(
            ApplicationDbContext ctx, List<ApplicationUser> users, List<Post> posts)
        {
            if (await ctx.Reports.AnyAsync()) { Console.WriteLine("[LoadSeeder]   Reports: already seeded."); return; }

            var reasons = Enum.GetValues<ReportReason>();
            var reports = new List<Report>();

            for (int i = 0; i < Math.Min(Cfg.PostReportCount, 2000); i++)
            {
                var post     = Pick(posts);
                var eligible = users.Where(u => u.Id != post.AuthorId).ToList();
                if (eligible.Count == 0) continue;

                reports.Add(new Report
                {
                    Id             = Guid.NewGuid(),
                    ReporterId     = Pick(eligible).Id,
                    ReportedPostId = post.Id,
                    Reason         = Pick(reasons),
                    Description    = "Continut raportat automat de seeder.",
                    Status         = Rng.NextDouble() < 0.6 ? ReportStatus.Pending : ReportStatus.Resolved,
                    CreatedAt      = DateTime.UtcNow.AddDays(-Rng.Next(1, 180))
                });
            }
            await BulkInsertAsync(ctx, reports, "Reports", "Reports");
        }

        private static async Task SeedNotificationsAsync(
            ApplicationDbContext ctx, List<ApplicationUser> users)
        {
            if (await ctx.Notifications.AnyAsync()) { Console.WriteLine("[LoadSeeder]   Notifications: already seeded."); return; }

            var types = new[]
            {
                ("Bine ai venit!",      "Exploreaza comunitatile si grupurile de studiu disponibile."),
                ("Raspuns nou",         "Cineva a raspuns la postarea ta."),
                ("Vot primit",          "Postarea ta a primit un vot pozitiv!"),
                ("Invitatie grup",      "Ai fost invitat sa te alturi unui grup de studiu."),
                ("Mesaj nou",           "Ai un mesaj direct nou."),
                ("Eveniment in curand", "Un eveniment la care esti inscris incepe in curand."),
                ("Insigna obtinuta",    "Felicitari! Ai obtinut o noua insigna de reputatie."),
                ("Postare populara",    "Postarea ta a depasit 100 de vizualizari!"),
            };
            var notifs = new List<Notification>();

            foreach (var user in users.Take(Math.Min(50_000, users.Count)))
            {
                int count = Rng.Next(2, Cfg.NotifPerUser + 3);
                for (int i = 0; i < count; i++)
                {
                    var (title, msg) = Pick(types);
                    notifs.Add(new Notification
                    {
                        Id        = Guid.NewGuid(),
                        UserId    = user.Id,
                        Title     = title,
                        Message   = msg,
                        IsRead    = Rng.NextDouble() > 0.4,
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 90))
                    });
                }
            }
            await BulkInsertAsync(ctx, notifs, "Notifications", "Notifications");
        }

        private static async Task SeedLearningResourcesAsync(
            ApplicationDbContext  ctx,
            List<ApplicationUser> users,
            List<StudyGroup>      groups,
            List<StoredFile>      files)
        {
            if (await ctx.LearningResources.AnyAsync()) { Console.WriteLine("[LoadSeeder]   LearningResources: already seeded."); return; }

            var resTypes  = new[] { "PDF","Video","Link","Article","Exercise","Slides","Notebook","Repository" };
            var resources = new List<LearningResource>();

            foreach (var g in groups)
            {
                for (int i = 0; i < Cfg.ResourcesPerGroup; i++)
                {
                    resources.Add(new LearningResource
                    {
                        Id           = Guid.NewGuid(),
                        GroupId      = g.Id,
                        UploaderId   = Pick(users).Id,
                        Title        = $"Resursa {i + 1} - {g.SubjectArea ?? g.Name}",
                        Description  = "Material util pentru pregatire si aprofundare.",
                        ResourceType = Pick(resTypes),
                        Url          = files.Any() && Rng.NextDouble() > 0.4
                                           ? Pick(files).FilePath
                                           : $"https://example.com/resource/{Guid.NewGuid():N}",
                        CreatedAt    = DateTime.UtcNow.AddDays(-Rng.Next(1, 365))
                    });
                }
            }
            await BulkInsertAsync(ctx, resources, "LearningResources", "LearningResources");
        }

        private static async Task SeedEventsAsync(
            ApplicationDbContext ctx, List<Community> communities, List<ApplicationUser> users)
        {
            if (await ctx.Events.AnyAsync()) { Console.WriteLine("[LoadSeeder]   Events: already seeded."); return; }

            var templates = new[]
            {
                ("Sesiune Q&A Live",    "Online (Microsoft Teams)", 1),
                ("Workshop Practic",    "Sala C201, ASE Bucuresti", 3),
                ("Hackathon 24h",       "Online (Discord)",        24),
                ("Conferinta Tehnica",  "Aula Magna, ASE",          8),
                ("Bootcamp Intensiv",   "Online (Zoom)",           16),
                ("Code Review Session", "Online (Meet)",            2),
                ("Meetup Comunitate",   "Hub-ul de Inovare",        4),
                ("Prezentare Proiecte", "Sala A101",                3),
            };
            var events = new List<Event>();

            foreach (var comm in communities)
            {
                for (int i = 0; i < Cfg.EventsPerCommunity; i++)
                {
                    var (title, loc, hours) = Pick(templates);
                    var start = DateTime.UtcNow.AddDays(Rng.Next(-60, 90));
                    events.Add(new Event
                    {
                        Id               = Guid.NewGuid(),
                        CommunityId      = comm.Id,
                        OrganizerId      = Pick(users).Id,
                        Title            = $"{comm.Name} - {title}",
                        Description      = $"Eveniment organizat pentru membrii comunitatii {comm.Name}. Participarea este gratuita.",
                        Location         = loc,
                        StartTime        = start,
                        EndTime          = start.AddHours(hours),
                        MaxAttendees     = Rng.Next(20, 500),
                        CurrentAttendees = Rng.Next(0, 200),
                        CreatedAt        = DateTime.UtcNow.AddDays(-Rng.Next(1, 60))
                    });
                }
            }
            await BulkInsertAsync(ctx, events, "Events", "Events");
        }

        private static async Task SeedGroupInvitesAsync(
            ApplicationDbContext ctx, List<StudyGroup> groups)
        {
            if (await ctx.GroupInvites.AnyAsync()) return;

            var invites = new List<GroupInvite>();
            foreach (var g in groups)
            {
                for (int i = 0; i < Cfg.InvitesPerGroup; i++)
                {
                    invites.Add(new GroupInvite
                    {
                        Id          = Guid.NewGuid(),
                        GroupId     = g.Id,
                        CreatorId   = g.OwnerId!,
                        Code        = $"INV-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                        CreatedAt   = DateTime.UtcNow.AddDays(-Rng.Next(1, 180)),
                        ExpiresAt   = DateTime.UtcNow.AddDays(Rng.Next(7, 90)),
                        MaxUses     = Rng.Next(5, 200),
                        CurrentUses = Rng.Next(0, 20)
                    });
                }
            }
            await BulkInsertAsync(ctx, invites, "GroupInvites", "GroupInvites");
        }

        // ─── SUMMARY ──────────────────────────────────────────────────────────────

        private static void PrintSummary()
        {
            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         ENTERPRISE LOAD-TEST SEED — COMPLETE                 ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
            Console.WriteLine($"║  Users           up to {Cfg.UserCount,7:N0}                              ║");
            Console.WriteLine($"║  Communities     {10,7}                                        ║");
            Console.WriteLine($"║  Study Groups    {12,7}                                        ║");
            Console.WriteLine($"║  Posts/Community {Cfg.PostsPerCommunity,7}                                        ║");
            Console.WriteLine($"║  Posts/Channel   {Cfg.PostsPerChannel,7}                                        ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║  PASSWORD: Test@1234!  (works for all seeded accounts)       ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        }
    }
}