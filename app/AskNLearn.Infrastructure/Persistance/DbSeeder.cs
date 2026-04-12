using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.Gamification;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.StudyGroup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AskNLearn.Infrastructure.Persistance
{
    public static class LoadTestDatabaseSeeder
    {
        // ─── CONFIG PENTRU ÎNCĂRCARE GREA ─────────────────────────────────────────
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

        // VALORI PENTRU SUTE DE MII DE ÎNREGISTRĂRI
        private static readonly ScaleConfig Cfg = new(
            UserCount: 100_000,
            PostsPerCommunity: 5_000,
            PostsPerChannel: 2_000,
            CommentsPerPost: 8,
            ChannelMessagesPerChannel: 3_000,
            MaxVotersPerPost: 100,
            MaxViewersPerPost: 500,
            DirectConvCount: 50_000,
            AuditLogCount: 20_000,
            PostReportCount: 8_000,
            NotifPerUser: 12,
            ResourcesPerGroup: 150,
            EventsPerCommunity: 25,
            InvitesPerGroup: 15,
            BatchSize: 5_000       // Batch optim pentru AddRange
        );

        // ─── RANDOM THREAD-SAFE ────────────────────────────────────────────────────
        private static readonly Random _globalRng = new();
        private static readonly ThreadLocal<Random> _tlsRng = new(() => new Random(_globalRng.Next()));
        private static Random Rng => _tlsRng.Value!;

        // ─── DATE FIXTURE ─────────────────────────────────────────────────────────
        private static readonly string[] FirstNames =
        [
            "Andrei","Maria","Alexandru","Elena","Stefan","Ioana","Mihai","Ana",
            "Cristian","Laura","Gabriel","Raluca","Ionut","Diana","Vlad","Andreea",
            "Matei","Simona","Claudiu","Monica","Bogdan","Teodora","Radu","Alina",
            "Cosmin","Bianca","Florin","Catalina","Sorin","Mirela","Octavian",
            "Luminita","Dragos","Roxana","Silviu","Corina","Adrian","Petronela",
            "Razvan","Gabriela","Marius","Oana","Lucian","Cristina","Tudor","Anca",
            "Ciprian","Mihaela","George","Roxana","Daniel","Nicoleta","Ionel","Loredana"
        ];

        private static readonly string[] LastNames =
        [
            "Popescu","Ionescu","Dumitrescu","Stan","Gheorghe","Rusu","Costin","Marin",
            "Tudor","Florescu","Nistor","Dobre","Barbu","Mihaila","Radulescu","Voinea",
            "Matei","Cristea","Diaconu","Enache","Badea","Iordache","Bucur","Vasilescu",
            "Lazar","Grigore","Ciobanu","Avram","Zaharia","Stanescu","Ungureanu",
            "Constantin","Neagu","Stoica","Manea","Olaru","Popa","Serban","Dragoi",
            "Chirila","Nita","Apostol","Bleotu","Coman","Dinu","Ene","Florea","Ghinea"
        ];

        private static readonly string[] Domains =
        [
            "@gmail.com","@yahoo.com","@outlook.com","@stud.ase.ro",
            "@csie.ase.ro","@upb.ro","@unibuc.ro","@student.ubbcluj.ro",
            "@info.uaic.ro","@student.uvt.ro"
        ];

        private static readonly string[] Occupations =
        [
            "Student","Master Student","PhD Student","Lecturer",
            "Researcher","Teaching Assistant","Industry Engineer",
            "Junior Developer","Senior Developer","Data Analyst",
            "DevOps Engineer","Cybersecurity Analyst","ML Engineer"
        ];

        private static readonly string[] Institutions =
        [
            "ASE Bucuresti","Politehnica Bucuresti","Universitatea din Bucuresti",
            "SNSPA","Academia Navala","Universitatea Babes-Bolyai",
            "Universitatea Tehnica Cluj","Universitatea de Vest Timisoara",
            "Universitatea Alexandru Ioan Cuza Iasi","Universitatea Tehnica Gh. Asachi Iasi"
        ];

        private static readonly string[] Interests =
        [
            "C#","Java","Python","Machine Learning","Web Development","DevOps",
            "Databases","Cybersecurity","Game Development","Mobile Apps","Cloud",
            "Algorithms","Data Science","IoT","Blockchain","Rust","Go","Kotlin",
            "Swift","TypeScript","React","Angular","Vue","Docker","Kubernetes"
        ];

        private static readonly string[] PostTitles =
        [
            "Cum rezolv problema N+1 in Entity Framework?",
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
            "GraphQL vs REST - comparatie detaliata",
            "Cum sa invat algoritmi eficient pentru interviuri?",
            "Diferenta dintre Stack si Heap in C#",
            "Ce este Dependency Injection si cum functioneaza?",
            "Cum sa fac debugging eficient in Visual Studio?",
            "Design Patterns esentiale pentru orice developer",
            "Cum sa construiesc un REST API bun in .NET?",
            "Entity Framework Core vs Dapper - care e mai bun?",
            "Cum sa implementez autorizare bazata pe roluri?",
            "Introducere in SOLID principles cu exemple practice",
            "Cum sa optimizez performanta unei aplicatii .NET?"
        ];

        private static readonly string[] PostContents =
        [
            "Am intalnit aceasta problema in proiectul meu si nu stiu cum sa o rezolv. Orice ajutor este binevenit!",
            "Dupa ce am studiat documentatia oficiala, am ajuns la concluzia ca cea mai buna abordare este urmatoarea...",
            "In cadrul cursului nostru, profesorul a mentionat aceasta tehnica dar nu a intrat in detalii. Cine poate explica?",
            "Am finalizat un proiect folosind aceasta tehnologie si pot sa impartasesc experienta mea cu voi.",
            "Cautam colegi pentru un proiect de grup la materia Baze de Date. Avem nevoie de 2-3 persoane.",
            "Am gasit o solutie eleganta pentru aceasta problema si vreau sa o impartasesc cu comunitatea.",
            "Cineva are resurse recomandate pentru a aprofunda acest subiect? Am nevoie de exemple practice.",
            "Aceasta este o intrebare mai avansata despre arhitectura sistemelor distribuite...",
            "Dupa multiple incercari, am reusit sa rezolv bug-ul. Iata pasii pe care i-am urmat.",
            "Vreau sa discut despre best practices in domeniu. Care este experienta voastra?",
            "Am citit mai multe articole despre acest subiect si am compilat un rezumat util pentru toata lumea.",
            "Lucrez la proiectul de licenta si am nevoie de pareri despre arhitectura aleasa.",
            "Caut colegi pentru a participa impreuna la un hackathon national luna viitoare.",
            "Impartasesc experienta mea dupa 6 luni de internship la o companie de software din Bucuresti.",
            "Am pregatit un tutorial pas cu pas pentru cei care incep sa invete aceasta tehnologie."
        ];

        private static readonly string[] CommentContents =
        [
            "Super explicatie, multumesc! M-a ajutat enorm.",
            "Am incercat si la mine merge perfect!",
            "Poti sa dai si un exemplu de cod mai detaliat?",
            "Eu am intalnit aceeasi problema saptamana trecuta si am rezolvat-o astfel...",
            "Nu sunt de acord complet, exista si alte aspecte de luat in considerare.",
            "Excelent post! Il salvez pentru referinta viitoare.",
            "Am mai multe intrebari, pot sa vin la consultatie sau sa te contactez pe privat?",
            "Confirm, am testat pe PostgreSQL 15 si functioneaza exact cum ai descris.",
            "Update: am rezolvat, era o problema de configurare in appsettings.",
            "Mersi pentru indicatie! M-a ajutat sa inteleg conceptul.",
            "Ai putea sa detaliezi pasul 3? Nu inteleg exact ce vrei sa spui acolo.",
            "Am implementat ceva similar in proiectul meu de licenta, pot sa impartasesc codul.",
            "Recomanzi vreo carte sau curs online pe acest subiect?",
            "Exact asta cautam! Multumesc pentru resursa valoroasa.",
            "Am o abordare alternativa care ar putea fi mai eficienta in anumite cazuri...",
            "Foarte bun post, l-am distribuit si colegilor mei din grupa.",
            "Poti sa explici si diferenta fata de abordarea clasica?",
            "Eu am facut asta intr-un mod diferit, dar rezultatul e acelasi.",
            "Ai cumva si un repository pe GitHub cu codul complet?",
            "Super util! Exact ce cautam pentru proiectul meu curent."
        ];

        private static readonly string[] ChannelMsgContents =
        [
            "Salut tuturor! Bine am gasit grupul!",
            "Cand avem urmatoarea sedinta de studiu?",
            "Am incarcat niste resurse noi in sectiunea de materiale.",
            "Reminder: maine avem deadline pentru tema!",
            "Cineva poate explica notiunile din capitolul 3?",
            "Am rezolvat exercitiul 5, vreti sa il discut?",
            "Multumesc pentru ajutor, am inteles acum!",
            "Propun o sesiune de recapitulare vineri seara la 19:00.",
            "Succes tuturor la examene!",
            "Felicitari echipei pentru release-ul de ieri!",
            "Cine vine la laboratorul optional de joi?",
            "Am gasit un articol interesant, il pun in resurse.",
            "Intrebare rapida: care e diferenta intre X si Y?",
            "Am terminat tema, daca vrea cineva sa faca code review.",
            "Meeting online duminica la 18:00, link in urmatorul mesaj.",
            "Tocmai am terminat de citit cartea recomandata, e extraordinara!",
            "Cineva are notitele de la cursul de ieri? Am lipsit.",
            "Am gasit un bug in codul din resurse, l-am corectat si am re-uploadat.",
            "Reminder ca maine avem prezentarea proiectelor, sa fim toti pregatiti!",
            "A aparut o noua versiune a framework-ului, merita verificata.",
            "Cine poate sa explice diferenta intre async si parallel in .NET?",
            "Am creat un grup de WhatsApp pentru comunicare rapida, cine vrea link?",
            "Felicitari lui @Andrei pentru rezolvarea problemei de ieri!",
            "Sesiunea de azi a fost foarte utila, multumesc tuturor!",
            "Urmatoarea sesiune o sa fie mai practica, pregatiti IDE-ul."
        ];

        private static readonly string[] DirectMsgContents =
        [
            "Salut! Ai putea sa ma ajuti cu ceva?",
            "Buna! Am vazut postarea ta si m-a interesat foarte mult.",
            "Hey, ai timp pentru o sesiune de studiu impreuna?",
            "Multumesc pentru raspunsul de la postare!",
            "Ai putea sa imi recomanzi niste resurse pentru incepatori?",
            "Buna ziua! Sunt interesat sa colaboram la un proiect.",
            "Am o intrebare despre codul pe care l-ai postat.",
            "Salut! Vrei sa facem parte din acelasi grup de studiu?",
            "Hey! Felicitari pentru postarea ta, a fost super utila.",
            "Buna! Sunt nou in comunitate, poate ne cunoastem.",
            "Am nevoie de ajutor urgent cu un bug, ai un minut?",
            "Salut! Am vazut ca esti din acelasi oras, poate ne intalnim.",
            "Buna! Lucrezi la ceva interesant in momentul de fata?",
            "Hey! Ti-am dat upvote la postare, era exact ce cautam.",
            "Salut! Esti disponibil pentru un scurt apel video sa discutam proiectul?"
        ];

        private static readonly string[] Emojis =
        [
            "thumbs_up","heart","laugh","party","thinking","eyes","fire",
            "check","hundred","wow","pray","muscle","target","rocket","star",
            "clap","raised_hands","ok_hand","wave","point_up"
        ];

        private static readonly string[] AuditActions =
        [
            "USER_CREATED","POST_DELETED","USER_BANNED","ROLE_CHANGED",
            "REPORT_RESOLVED","CONTENT_FLAGGED","GROUP_CREATED","MEMBER_KICKED",
            "POST_PINNED","USER_VERIFIED","COMMUNITY_CREATED","CHANNEL_DELETED",
            "MESSAGE_DELETED","USER_UNBANNED","POST_LOCKED","REPORT_DISMISSED"
        ];

        public const string DefaultPassword = "Test@1234!";

        // ─── ENTRY POINT ──────────────────────────────────────────────────────────
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, bool force = false)
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"[LoadSeeder] STARTING ENTERPRISE SEED (force={force})");
            Console.WriteLine($"[LoadSeeder] Config: {Cfg.UserCount:N0} users, {Cfg.ChannelMessagesPerChannel:N0}+ msgs/channel");
            Console.WriteLine($"[LoadSeeder] Batch size: {Cfg.BatchSize:N0}");

            try
            {
                if (force)
                {
                    await ClearAllDataAsync(context);
                }

                await SeedRanksAsync(context);
                var tags = await SeedTagsAsync(context, force);
                var users = await SeedUsersAsync(context, force);
                await SeedVerificationRequestsAsync(context, users, force);
                var files = await SeedStoredFilesAsync(context, users, force);
                await SeedFriendshipsAsync(context, users, force);
                var communities = await SeedCommunitiesAsync(context, users, tags, force);
                var groups = await SeedStudyGroupsAsync(context, users, force);
                await SeedGroupRolesAndMembershipsAsync(context, groups, users, force);
                await SeedChannelCategoriesAsync(context, groups, force);
                var channels = await SeedChannelsAsync(context, groups, force);
                var posts = await SeedPostsAsync(context, communities, channels, users, tags, force);
                var messages = await SeedMessagesAsync(context, posts, channels, users, force);
                await SeedDirectMessagingAsync(context, users, force);
                await SeedPostVotesAsync(context, posts, users, force);
                await SeedPostViewsAsync(context, posts, users, force);
                await SeedMessageReactionsAsync(context, messages, users, force);
                await SeedAuditLogsAsync(context, users, force);
                await SeedReportsAsync(context, users, posts, messages, force);
                await SeedNotificationsAsync(context, users, force);
                await SeedLearningResourcesAsync(context, users, groups, files, force);
                await SeedEventsAsync(context, communities, users, force);
                await SeedGroupInvitesAsync(context, groups, force);

                sw.Stop();
                Console.WriteLine($"\n[LoadSeeder] COMPLET în {sw.Elapsed.TotalMinutes:F1} minute.");
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

        // ─── CLEAR ALL TABLES ─────────────────────────────────────────────────────
        private static async Task ClearAllDataAsync(ApplicationDbContext ctx)
        {
            Console.WriteLine("[LoadSeeder] Ștergere tabele...");
            var tables = new[]
            {
                "AuditLogs", "Reports", "Notifications", "MessageReactions", "MessageAttachments", "Messages",
                "DirectConversationParticipants", "DirectConversations", "PostTags", "PostVotes", "PostViews",
                "PostAttachments", "Posts", "LearningResources", "Events", "GroupInvites", "GroupMemberships",
                "Channels", "ChannelCategories", "GroupRoles", "StudyGroups", "Friendships", "CommunityMemberships",
                "Communities", "StoredFiles", "VerificationRequests", "UserRoles", "UserClaims", "UserLogins",
                "UserTokens", "RoleClaims", "Roles", "Users", "UserRanks", "Tags", "__EFMigrationsHistory"
            };

            foreach (var table in tables)
            {
                try
                {
                    await ctx.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}]");
                }
                catch
                {
                    // Ignorăm erorile - tabelul poate să nu existe
                }
            }
            Console.WriteLine("[LoadSeeder] Tabele golite.");
        }

        // ─── BATCH INSERT (SIGUR, FĂRĂ SQLBULKCOPY) ───────────────────────────────
        private static async Task BatchInsertAsync<T>(ApplicationDbContext ctx, List<T> entities, string label) where T : class
        {
            if (entities.Count == 0) return;

            int total = entities.Count;
            int processed = 0;

            Console.Write($"[LoadSeeder]   {label}: {total:N0} entități...");

            for (int i = 0; i < total; i += Cfg.BatchSize)
            {
                var batch = entities.Skip(i).Take(Cfg.BatchSize).ToList();
                await ctx.Set<T>().AddRangeAsync(batch);

                try
                {
                    await ctx.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[LoadSeeder] ERROR la batch-ul {i / Cfg.BatchSize + 1}: {ex.Message}");
                    throw;
                }

                ctx.ChangeTracker.Clear();
                processed += batch.Count;

                if (total > 10000 && processed % 10000 == 0)
                {
                    Console.Write($"\r[LoadSeeder]   {label}: {processed:N0}/{total:N0}...");
                }
            }

            Console.WriteLine($"\r[LoadSeeder]   {label}: {total:N0} - COMPLET");
        }

        // ─── HELPERS ──────────────────────────────────────────────────────────────
        private static T Pick<T>(IReadOnlyList<T> list) => list[Rng.Next(list.Count)];
        private static T Pick<T>(T[] arr) => arr[Rng.Next(arr.Length)];

        private static List<T> PickMany<T>(IReadOnlyList<T> list, int n)
        {
            var copy = list.ToList();
            var result = new List<T>(n);
            for (int i = 0; i < n && copy.Count > 0; i++)
            {
                int idx = Rng.Next(copy.Count);
                result.Add(copy[idx]);
                copy.RemoveAt(idx);
            }
            return result;
        }

        private static string HashPassword(string password)
            => new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), password);

        // ─── SEED METHODS ─────────────────────────────────────────────────────────
        private static async Task<List<UserRank>> SeedRanksAsync(ApplicationDbContext ctx, bool force = false)
        {
            if (!force && await ctx.UserRanks.AnyAsync()) return await ctx.UserRanks.AsNoTracking().ToListAsync();

            var ranks = new List<UserRank>
            {
                new() { Id = Guid.NewGuid(), Name = "Novice", MinPoints = 0, IconUrl = "/icons/ranks/novice.png" },
                new() { Id = Guid.NewGuid(), Name = "Apprentice", MinPoints = 500, IconUrl = "/icons/ranks/apprentice.png" },
                new() { Id = Guid.NewGuid(), Name = "Scholar", MinPoints = 1500, IconUrl = "/icons/ranks/scholar.png" },
                new() { Id = Guid.NewGuid(), Name = "Expert", MinPoints = 3500, IconUrl = "/icons/ranks/expert.png" },
                new() { Id = Guid.NewGuid(), Name = "Master", MinPoints = 7500, IconUrl = "/icons/ranks/master.png" },
                new() { Id = Guid.NewGuid(), Name = "Legend", MinPoints = 15000, IconUrl = "/icons/ranks/legend.png" },
            };

            await ctx.UserRanks.AddRangeAsync(ranks);
            await ctx.SaveChangesAsync();
            Console.WriteLine($"[LoadSeeder]   Ranks: {ranks.Count}");
            return ranks;
        }

        private static async Task<List<Tag>> SeedTagsAsync(ApplicationDbContext ctx, bool force = false)
        {
            if (!force && await ctx.Tags.AnyAsync()) return await ctx.Tags.AsNoTracking().ToListAsync();

            var names = new[]
            {
                "Programming","C#","SQL","Machine Learning","Economics","Statistics",
                "Web Development","Database","AI","Cloud","Security","Finance",
                "Management","Marketing","Research","Algorithms","Data Structures",
                "DevOps","Mobile","UI/UX","Networking","Operating Systems",
                "Python","Java","Docker","Kubernetes","React","Angular","Vue",
                "TypeScript","Rust","Go","Microservices","Testing","Architecture"
            };

            var tags = names.Select(n => new Tag { Id = Guid.NewGuid(), Name = n, UsageCount = Rng.Next(0, 5000) }).ToList();
            await ctx.Tags.AddRangeAsync(tags);
            await ctx.SaveChangesAsync();
            Console.WriteLine($"[LoadSeeder]   Tags: {tags.Count}");
            return tags;
        }

        private static async Task<List<ApplicationUser>> SeedUsersAsync(ApplicationDbContext ctx, bool force = false)
        {
            int existing = await ctx.Users.CountAsync();
            if (!force && existing >= 1000)
            {
                Console.WriteLine($"[LoadSeeder]   Users: already {existing:N0}");
                return await ctx.Users.AsNoTracking().ToListAsync();
            }

            var sw = Stopwatch.StartNew();
            var hash = HashPassword(DefaultPassword);
            var ranks = await ctx.UserRanks.AsNoTracking().ToListAsync();

            int[] repBuckets = [10, 25, 50, 80, 120, 180, 250, 350, 500, 700, 900, 1200, 1600, 2200, 3000, 4000, 5500, 8000, 12000, 20000];
            double[] repWeights = [0.20, 0.18, 0.15, 0.12, 0.09, 0.07, 0.05, 0.04, 0.03, 0.02, 0.015, 0.012, 0.010, 0.008, 0.006, 0.004, 0.003, 0.002, 0.001, 0.0005];

            int GetWeightedRep()
            {
                double roll = Rng.NextDouble();
                double cum = 0;
                for (int i = 0; i < repWeights.Length; i++)
                {
                    cum += repWeights[i];
                    if (roll < cum) return repBuckets[i] + Rng.Next(-20, 200);
                }
                return repBuckets[^1] + Rng.Next(0, 5000);
            }

            UserRank GetRankForRep(int rep) =>
                ranks.OrderByDescending(r => r.MinPoints).FirstOrDefault(r => rep >= r.MinPoints)
                ?? ranks.OrderBy(r => r.MinPoints).First();

            var bag = new List<ApplicationUser>();

            for (int i = 0; i < Cfg.UserCount; i++)
            {
                var first = Pick(FirstNames);
                var last = Pick(LastNames);
                var email = $"{first.ToLower()}.{last.ToLower()}{i}{Pick(Domains)}";
                double r = Rng.NextDouble();
                var role = r < 0.02 ? Role.Admin : r < 0.07 ? Role.Moderator : Role.Member;
                var rep = GetWeightedRep();
                var verified = role != Role.Member || (rep > 800 && Rng.NextDouble() < 0.65);
                var rank = GetRankForRep(rep);
                var created = DateTime.UtcNow.AddMonths(-Rng.Next(1, 48));

                bag.Add(new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = email,
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    NormalizedUserName = email.ToUpperInvariant(),
                    FullName = $"{first} {last}",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = false,
                    LockoutEnabled = true,
                    AccessFailedCount = 0,
                    Role = role,
                    IsVerified = verified,
                    ReputationPoints = rep,
                    CurrentRankId = rank.Id,
                    Bio = $"{first} {last} este {Pick(Occupations)} la {Pick(Institutions)}. Pasionat de {Pick(Interests)} si {Pick(Interests)}.",
                    Institution = Pick(Institutions),
                    Occupation = Pick(Occupations),
                    Interests = string.Join(", ", PickMany(Interests, Rng.Next(2, 6))),
                    SocialLinks = $"{{\"github\":\"https://github.com/{first.ToLower()}{i}\"}}",
                    AvatarUrl = $"https://randomuser.me/api/portraits/{(Rng.Next(2) == 0 ? "men" : "women")}/{Rng.Next(1, 99)}.jpg",
                    Status = Pick(new[] { "Online", "Offline", "Away" }),
                    LastActive = DateTime.UtcNow.AddHours(-Rng.Next(1, 4320)),
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    CreatedAt = created,
                    PasswordHash = hash
                });
            }

            await BatchInsertAsync(ctx, bag, "Users");
            Console.WriteLine($"[LoadSeeder]   Users total: {bag.Count:N0} în {sw.Elapsed.TotalSeconds:F1}s");
            return bag;
        }

        private static async Task SeedVerificationRequestsAsync(ApplicationDbContext ctx, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.VerificationRequests.AnyAsync()) return;

            var admins = users.Where(u => u.Role == Role.Admin || u.Role == Role.Moderator).Take(20).ToList();
            var reqs = new List<VerificationRequest>();

            foreach (var user in users.Where(u => u.IsVerified).Take(8000))
            {
                var submitted = DateTime.UtcNow.AddDays(-Rng.Next(10, 365));
                reqs.Add(new VerificationRequest
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    StudentIdUrl = $"/uploads/verification/id_{Guid.NewGuid():N}.jpg",
                    CarnetUrl = $"/uploads/verification/carnet_{Guid.NewGuid():N}.jpg",
                    Status = Status.Approved,
                    ProcessedBy = admins.Any() ? Pick(admins).Id : user.Id,
                    ProcessedAt = submitted.AddDays(Rng.Next(1, 14)),
                    SubmittedAt = submitted
                });
            }

            foreach (var user in users.Where(u => !u.IsVerified && u.ReputationPoints > 300).Take(2000))
            {
                reqs.Add(new VerificationRequest
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    StudentIdUrl = $"/uploads/verification/id_{Guid.NewGuid():N}.jpg",
                    CarnetUrl = $"/uploads/verification/carnet_{Guid.NewGuid():N}.jpg",
                    Status = Status.Pending,
                    SubmittedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 30))
                });
            }

            await BatchInsertAsync(ctx, reqs, "VerificationRequests");
        }

        private static async Task<List<StoredFile>> SeedStoredFilesAsync(ApplicationDbContext ctx, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.StoredFiles.AnyAsync()) return await ctx.StoredFiles.AsNoTracking().ToListAsync();

            var exts = new[] { "pdf", "docx", "xlsx", "pptx", "zip", "png", "jpg", "mp4", "csv", "ipynb", "md", "txt" };
            var modules = new[] { "posts", "groups", "profile", "resources", "messages" };
            var files = new List<StoredFile>();

            for (int i = 0; i < 10_000; i++)
            {
                var ext = Pick(exts);
                files.Add(new StoredFile
                {
                    Id = Guid.NewGuid(),
                    FileName = $"document_{i:D6}.{ext}",
                    FilePath = $"/uploads/files/{Guid.NewGuid():N}.{ext}",
                    FileType = $"application/{ext}",
                    FileSize = Rng.Next(1024, 50 * 1024 * 1024),
                    ModuleContext = Pick(modules),
                    UploaderId = Pick(users).Id,
                    UploadedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 730))
                });
            }

            await BatchInsertAsync(ctx, files, "StoredFiles");
            return files;
        }

        private static async Task SeedFriendshipsAsync(ApplicationDbContext ctx, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.Friendships.AnyAsync()) return;

            var friendships = new List<Friendship>();
            var seen = new HashSet<string>();
            var sample = users.Take(Math.Min(50_000, users.Count)).ToList();
            int n = sample.Count;

            foreach (var (user, idx) in sample.Select((u, i) => (u, i)))
            {
                int clusterSize = Rng.Next(8, 20);
                int start = Math.Max(0, idx - clusterSize);
                int end = Math.Min(n - 1, idx + clusterSize);

                for (int j = start; j <= end; j++)
                {
                    if (j == idx) continue;
                    var other = sample[j];
                    var key = string.Compare(user.Id, other.Id, StringComparison.Ordinal) < 0
                        ? $"{user.Id}|{other.Id}" : $"{other.Id}|{user.Id}";
                    if (!seen.Add(key)) continue;

                    double roll = Rng.NextDouble();
                    var status = roll < 0.85 ? FriendshipStatus.Accepted
                                : roll < 0.95 ? FriendshipStatus.Pending
                                : FriendshipStatus.Declined;

                    friendships.Add(new Friendship
                    {
                        RequesterId = user.Id,
                        AddresseeId = other.Id,
                        Status = status,
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 730))
                    });
                }

                for (int r = 0; r < Rng.Next(2, 8); r++)
                {
                    var other = sample[Rng.Next(n)];
                    if (other.Id == user.Id) continue;
                    var key = string.Compare(user.Id, other.Id, StringComparison.Ordinal) < 0
                        ? $"{user.Id}|{other.Id}" : $"{other.Id}|{user.Id}";
                    if (!seen.Add(key)) continue;

                    friendships.Add(new Friendship
                    {
                        RequesterId = user.Id,
                        AddresseeId = other.Id,
                        Status = Rng.NextDouble() < 0.70 ? FriendshipStatus.Accepted : FriendshipStatus.Pending,
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 365))
                    });
                }
            }

            await BatchInsertAsync(ctx, friendships, "Friendships");
        }

        private static async Task<List<Community>> SeedCommunitiesAsync(ApplicationDbContext ctx, List<ApplicationUser> users, List<Tag> tags, bool force = false)
        {
            if (!force && await ctx.Communities.AnyAsync()) return await ctx.Communities.AsNoTracking().ToListAsync();

            var defs = new[]
            {
                ("Informatica Economica", "ineconomica", "Comunitate dedicata studentilor de la Informatica Economica"),
                ("Cibernetica si Statistica", "cibernetica", "Matematica aplicata, statistica si modelare economica"),
                ("Inginerie Software", "ing-software", "Arhitecturi, design patterns, best practices in software"),
                ("Machine Learning & AI", "ml-ai", "Invatare automata, retele neuronale si inteligenta artificiala"),
                ("Securitate Informatica", "securitate", "Cybersecurity, penetration testing, CTF competitions"),
                ("Web & Mobile Development", "web-mobile", "Frontend, backend, aplicatii mobile native si cross-platform"),
                ("Data Science", "data-science", "Analiza de date, vizualizare, business intelligence"),
                ("Cloud & DevOps", "cloud-devops", "AWS, Azure, GCP, Kubernetes, CI/CD pipelines"),
                ("Competitive Programming", "competitive", "Algoritmi, structuri de date, concursuri de programare"),
                ("Open Source Romania", "open-source", "Proiecte open source, contributii, comunitate"),
            };

            var staff = users.Where(u => u.Role != Role.Member).Take(20).ToList();
            var communities = defs.Select(d => new Community
            {
                Id = Guid.NewGuid(),
                Name = d.Item1,
                Slug = d.Item2,
                Description = d.Item3,
                ImageUrl = $"/images/communities/{d.Item2}.png",
                CreatorId = Pick(staff).Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-Rng.Next(3, 48))
            }).ToList();

            await ctx.Communities.AddRangeAsync(communities);
            await ctx.SaveChangesAsync();

            var memberships = new List<CommunityMembership>();
            var memberSet = new HashSet<string>();

            foreach (var user in users.Take(50_000))
            {
                int joins = Rng.Next(2, 6);
                foreach (var comm in communities.OrderBy(_ => Rng.Next()).Take(joins))
                {
                    var key = $"{comm.Id}|{user.Id}";
                    if (!memberSet.Add(key)) continue;

                    var isStaff = staff.Any(s => s.Id == user.Id);
                    memberships.Add(new CommunityMembership
                    {
                        CommunityId = comm.Id,
                        UserId = user.Id,
                        Role = isStaff ? CommunityRole.Moderator : CommunityRole.Member,
                        IsMuted = Rng.NextDouble() < 0.02,
                        JoinedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 730))
                    });
                }
            }

            await BatchInsertAsync(ctx, memberships, "CommunityMemberships");
            Console.WriteLine($"[LoadSeeder]   Communities: {communities.Count} | Memberships: {memberships.Count:N0}");
            return communities;
        }

        private static async Task<List<StudyGroup>> SeedStudyGroupsAsync(ApplicationDbContext ctx, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.StudyGroups.AnyAsync()) return await ctx.StudyGroups.AsNoTracking().ToListAsync();

            var defs = new[]
            {
                ("Pregatire Licenta 2025", "Pregatire intensiva pentru examenul de licenta si disertatie"),
                ("DotNet Advanced", "Concepte avansate de C# si ecosistemul .NET 8/9"),
                ("C/C++ Algorithms", "Competitive programming si algoritmi clasici in C++"),
                ("SQL Mastery", "Optimizare, indexare si tuning pentru PostgreSQL si SQL Server"),
                ("DevOps & Cloud Native", "Docker, Kubernetes, CI/CD, AWS, Azure, Terraform"),
                ("React & TypeScript", "Frontend modern cu React 18, TypeScript si Next.js"),
                ("Python & Data Science", "NumPy, Pandas, Scikit-learn, TensorFlow, PyTorch"),
                ("Mobile Dev - Flutter", "Cross-platform mobile cu Flutter, Dart si Firebase"),
                ("Cybersecurity Basics", "Introducere in securitate, CTF-uri si ethical hacking"),
                ("System Design", "Proiectarea sistemelor scalabile, microservices, caching"),
                ("Open Source Contributors", "Contributii la proiecte open source, code review"),
                ("Game Development Unity", "Dezvoltare jocuri 2D/3D cu Unity si C#"),
            };

            var staff = users.Where(u => u.Role != Role.Member).Take(20).ToList();
            var groups = defs.Select(d => new StudyGroup
            {
                Id = Guid.NewGuid(),
                Name = d.Item1,
                Description = d.Item2,
                OwnerId = Pick(staff).Id,
                IsPublic = Rng.NextDouble() > 0.25,
                SubjectArea = Pick(Interests),
                InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                CreatedAt = DateTime.UtcNow.AddMonths(-Rng.Next(1, 36))
            }).ToList();

            await ctx.StudyGroups.AddRangeAsync(groups);
            await ctx.SaveChangesAsync();
            Console.WriteLine($"[LoadSeeder]   StudyGroups: {groups.Count}");
            return groups;
        }

        private static async Task SeedGroupRolesAndMembershipsAsync(ApplicationDbContext ctx, List<StudyGroup> groups, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.GroupRoles.AnyAsync()) return;

            var allRoles = new List<GroupRole>();
            foreach (var g in groups)
            {
                allRoles.Add(new GroupRole { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Admin", Color = "#FF6B6B", Permissions = "ALL", Priority = 100 });
                allRoles.Add(new GroupRole { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Moderator", Color = "#4ECDC4", Permissions = "MANAGE_MESSAGES,KICK", Priority = 50 });
                allRoles.Add(new GroupRole { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Member", Color = "#45B7D1", Permissions = "READ,WRITE", Priority = 10 });
                allRoles.Add(new GroupRole { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Guest", Color = "#96CEB4", Permissions = "READ", Priority = 1 });
            }
            await ctx.GroupRoles.AddRangeAsync(allRoles);
            await ctx.SaveChangesAsync();

            var adminRoles = allRoles.Where(r => r.Name == "Admin").ToList();
            var modRoles = allRoles.Where(r => r.Name == "Moderator").ToList();
            var memberRoles = allRoles.Where(r => r.Name == "Member").ToList();
            var memberships = new List<GroupMembership>();
            var memSet = new HashSet<string>();

            foreach (var g in groups)
            {
                var adminRole = adminRoles.First(r => r.GroupId == g.Id);
                var modRole = modRoles.First(r => r.GroupId == g.Id);
                var memberRole = memberRoles.First(r => r.GroupId == g.Id);

                if (g.OwnerId != null && memSet.Add($"{g.Id}|{g.OwnerId}"))
                    memberships.Add(new GroupMembership { GroupId = g.Id, UserId = g.OwnerId, GroupRoleId = adminRole.Id, IsBanned = false, JoinedAt = g.CreatedAt });

                int modCount = Rng.Next(3, 6);
                foreach (var u in users.Where(u => u.Id != g.OwnerId && u.Role == Role.Moderator).Take(modCount))
                    if (memSet.Add($"{g.Id}|{u.Id}"))
                        memberships.Add(new GroupMembership { GroupId = g.Id, UserId = u.Id, GroupRoleId = modRole.Id, IsBanned = false, JoinedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 365)) });

                int memberCount = Rng.Next(800, 2000);
                foreach (var u in users.Where(u => u.Id != g.OwnerId).OrderBy(_ => Rng.Next()).Take(memberCount))
                    if (memSet.Add($"{g.Id}|{u.Id}"))
                        memberships.Add(new GroupMembership { GroupId = g.Id, UserId = u.Id, GroupRoleId = memberRole.Id, IsBanned = Rng.NextDouble() < 0.005, JoinedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 730)) });
            }

            await BatchInsertAsync(ctx, memberships, "GroupMemberships");
            Console.WriteLine($"[LoadSeeder]   GroupMemberships: {memberships.Count:N0}");
        }

        private static async Task SeedChannelCategoriesAsync(ApplicationDbContext ctx, List<StudyGroup> groups, bool force = false)
        {
            if (!force && await ctx.ChannelCategories.AnyAsync()) return;

            var cats = groups.SelectMany(g => new[]
            {
                new ChannelCategory { Id = Guid.NewGuid(), GroupId = g.Id, Name = "General", Position = 0 },
                new ChannelCategory { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Resurse", Position = 1 },
                new ChannelCategory { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Proiecte", Position = 2 },
                new ChannelCategory { Id = Guid.NewGuid(), GroupId = g.Id, Name = "Off-Topic", Position = 3 }
            }).ToList();

            await BatchInsertAsync(ctx, cats, "ChannelCategories");
        }

        private static async Task<List<Channel>> SeedChannelsAsync(ApplicationDbContext ctx, List<StudyGroup> groups, bool force = false)
        {
            if (!force && await ctx.Channels.AnyAsync()) return await ctx.Channels.AsNoTracking().ToListAsync();

            var categories = await ctx.ChannelCategories.AsNoTracking().ToListAsync();
            var channels = new List<Channel>();

            foreach (var g in groups)
            {
                Guid? catGen = categories.FirstOrDefault(c => c.GroupId == g.Id && c.Name == "General")?.Id;
                Guid? catRes = categories.FirstOrDefault(c => c.GroupId == g.Id && c.Name == "Resurse")?.Id;
                Guid? catProj = categories.FirstOrDefault(c => c.GroupId == g.Id && c.Name == "Proiecte")?.Id;
                Guid? catOff = categories.FirstOrDefault(c => c.GroupId == g.Id && c.Name == "Off-Topic")?.Id;

                int pos = 0;
                channels.AddRange(new[]
                {
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catGen, Name = "general", Type = ChannelType.Text, Topic = "Discutii generale", IsPrivate = false, Position = pos++ },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catGen, Name = "anunturi", Type = ChannelType.Text, Topic = "Anunturi importante", IsPrivate = false, Position = pos++ },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catOff, Name = "off-topic", Type = ChannelType.Text, Topic = "Subiecte neoficiale", IsPrivate = false, Position = pos++ },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catOff, Name = "fun-memes", Type = ChannelType.Text, Topic = "Meme-uri si glume", IsPrivate = false, Position = pos++ },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catRes, Name = "resurse", Type = ChannelType.Text, Topic = "Materiale si link-uri", IsPrivate = false, Position = pos++ },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catRes, Name = "carti-cursuri", Type = ChannelType.Text, Topic = "Carti si cursuri recomandate", IsPrivate = false, Position = pos++ },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catProj, Name = "proiecte", Type = ChannelType.Text, Topic = "Colaborare proiecte", IsPrivate = false, Position = pos++ },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catProj, Name = "code-review", Type = ChannelType.Text, Topic = "Review cod si PR-uri", IsPrivate = false, Position = pos++ },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catGen, Name = "Voice General", Type = ChannelType.Voice, IsPrivate = false, Position = pos++ },
                    new Channel { Id = Guid.NewGuid(), GroupId = g.Id, CategoryId = catProj, Name = "Voice Proiecte", Type = ChannelType.Voice, IsPrivate = false, Position = pos },
                });
            }

            await BatchInsertAsync(ctx, channels, "Channels");
            Console.WriteLine($"[LoadSeeder]   Channels: {channels.Count}");
            return channels;
        }

        private static async Task<List<Post>> SeedPostsAsync(ApplicationDbContext ctx, List<Community> communities, List<Channel> channels, List<ApplicationUser> users, List<Tag> tags, bool force = false)
        {
            if (!force && await ctx.Posts.AnyAsync())
            {
                Console.WriteLine("[LoadSeeder]   Posts: already seeded.");
                return await ctx.Posts.AsNoTracking().Take(20_000).ToListAsync();
            }

            var posts = new List<Post>();
            var postTags = new List<PostTag>();
            var tagSeen = new HashSet<string>();

            foreach (var comm in communities)
            {
                for (int i = 0; i < Cfg.PostsPerCommunity; i++)
                {
                    var post = new Post
                    {
                        Id = Guid.NewGuid(),
                        CommunityId = comm.Id,
                        AuthorId = Pick(users).Id,
                        Title = Pick(PostTitles),
                        Content = Pick(PostContents),
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 730)),
                        IsSolved = Rng.NextDouble() > 0.55,
                        IsPinned = Rng.NextDouble() > 0.94,
                        IsLocked = Rng.NextDouble() > 0.97,
                        ViewCount = Rng.Next(0, 5000),
                        ModerationStatus = ModerationStatus.Approved,
                    };
                    posts.Add(post);

                    foreach (var tag in PickMany(tags, Rng.Next(1, 5)))
                    {
                        if (tagSeen.Add($"{post.Id}|{tag.Id}"))
                            postTags.Add(new PostTag { PostId = post.Id, TagId = tag.Id });
                    }
                }
            }

            var textChannels = channels.Where(c => c.Type == ChannelType.Text).ToList();
            foreach (var chan in textChannels)
            {
                for (int i = 0; i < Cfg.PostsPerChannel; i++)
                {
                    posts.Add(new Post
                    {
                        Id = Guid.NewGuid(),
                        ChannelId = chan.Id,
                        AuthorId = Pick(users).Id,
                        Title = Pick(PostTitles),
                        Content = Pick(PostContents),
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 365)),
                        IsSolved = false,
                        IsPinned = false,
                        IsLocked = false,
                        ViewCount = Rng.Next(0, 500),
                        ModerationStatus = ModerationStatus.Approved,
                    });
                }
            }

            await BatchInsertAsync(ctx, posts, "Posts");
            await BatchInsertAsync(ctx, postTags, "PostTags");
            Console.WriteLine($"[LoadSeeder]   Posts: {posts.Count:N0} | PostTags: {postTags.Count:N0}");
            return posts;
        }

        private static async Task<List<Message>> SeedMessagesAsync(ApplicationDbContext ctx, List<Post> posts, List<Channel> channels, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.Messages.AnyAsync())
            {
                Console.WriteLine("[LoadSeeder]   Messages: already seeded.");
                return await ctx.Messages.AsNoTracking().Take(20_000).ToListAsync();
            }

            var messages = new List<Message>();
            var communityPosts = posts.Where(p => p.CommunityId != null).ToList();

            // Comentarii la postări
            foreach (var post in communityPosts)
            {
                int count = Rng.Next(2, Cfg.CommentsPerPost + 1);
                for (int i = 0; i < count; i++)
                {
                    messages.Add(new Message
                    {
                        Id = Guid.NewGuid(),
                        PostId = post.Id,
                        AuthorId = Pick(users).Id,
                        Content = Pick(CommentContents),
                        CreatedAt = post.CreatedAt.AddMinutes(Rng.Next(1, 30_000)),
                        IsPinned = Rng.NextDouble() > 0.97,
                        IsEdited = Rng.NextDouble() > 0.85,
                        ModerationStatus = ModerationStatus.Approved,
                    });
                }
            }

            // MESAJE PE CANALE – BUG REPARAT (min > max)
            var textChannels = channels.Where(c => c.Type == ChannelType.Text).ToList();
            Console.WriteLine($"[LoadSeeder]   Generare mesaje pentru {textChannels.Count} canale text...");

            long totalMessages = 0;
            foreach (var chan in textChannels)
            {
                // REPARAȚIE: Cfg.ChannelMessagesPerChannel este minimul, adăugăm variație
                int count = Cfg.ChannelMessagesPerChannel + Rng.Next(0, 2000);
                for (int i = 0; i < count; i++)
                {
                    messages.Add(new Message
                    {
                        Id = Guid.NewGuid(),
                        ChannelId = chan.Id,
                        AuthorId = Pick(users).Id,
                        Content = Pick(ChannelMsgContents),
                        CreatedAt = DateTime.UtcNow.AddHours(-Rng.Next(1, 17_520)),
                        IsPinned = Rng.NextDouble() > 0.98,
                        IsEdited = Rng.NextDouble() > 0.90,
                        ModerationStatus = ModerationStatus.Approved,
                    });
                }
                totalMessages += count;
            }
            Console.WriteLine($"[LoadSeeder]   Total mesaje canal: {totalMessages:N0}");

            await BatchInsertAsync(ctx, messages, "Messages");
            Console.WriteLine($"[LoadSeeder]   Total messages (incl. comments): {messages.Count:N0}");
            return messages;
        }

        private static async Task SeedDirectMessagingAsync(ApplicationDbContext ctx, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.DirectConversations.AnyAsync()) return;

            var seen = new HashSet<string>();
            var conversations = new List<DirectConversation>();
            var participants = new List<DirectConversationParticipant>();
            var dmMessages = new List<Message>();

            var acceptedFriendships = await ctx.Friendships
                .Where(f => f.Status == FriendshipStatus.Accepted)
                .AsNoTracking()
                .Take(10_000)
                .ToListAsync();

            foreach (var friendship in acceptedFriendships.Take(Cfg.DirectConvCount))
            {
                var key = string.Compare(friendship.RequesterId, friendship.AddresseeId, StringComparison.Ordinal) < 0
                    ? $"{friendship.RequesterId}|{friendship.AddresseeId}"
                    : $"{friendship.AddresseeId}|{friendship.RequesterId}";
                if (!seen.Add(key)) continue;

                var cid = Guid.NewGuid();
                conversations.Add(new DirectConversation { Id = cid, CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 365)) });
                participants.Add(new DirectConversationParticipant { ConversationId = cid, UserId = friendship.RequesterId });
                participants.Add(new DirectConversationParticipant { ConversationId = cid, UserId = friendship.AddresseeId });

                int msgCount = Rng.Next(2, 21);
                for (int m = 0; m < msgCount; m++)
                {
                    var senderId = Rng.NextDouble() < 0.5 ? friendship.RequesterId : friendship.AddresseeId;
                    dmMessages.Add(new Message
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = cid,
                        AuthorId = senderId,
                        Content = Pick(DirectMsgContents),
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 180)).AddMinutes(m * Rng.Next(1, 120)),
                        IsPinned = false,
                        IsEdited = Rng.NextDouble() > 0.95,
                        ModerationStatus = ModerationStatus.Approved,
                    });
                }
            }

            for (int i = conversations.Count; i < Math.Min(Cfg.DirectConvCount, 8000); i++)
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

                int msgCount = Rng.Next(1, 10);
                for (int m = 0; m < msgCount; m++)
                {
                    dmMessages.Add(new Message
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = cid,
                        AuthorId = Rng.NextDouble() < 0.5 ? u1.Id : u2.Id,
                        Content = Pick(DirectMsgContents),
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 180)).AddMinutes(m * 10),
                        IsPinned = false,
                        IsEdited = false,
                        ModerationStatus = ModerationStatus.Approved,
                    });
                }
            }

            await BatchInsertAsync(ctx, conversations, "DirectConversations");
            await BatchInsertAsync(ctx, participants, "DirectConversationParticipants");
            await BatchInsertAsync(ctx, dmMessages, "DM Messages");
            Console.WriteLine($"[LoadSeeder]   DirectConversations: {conversations.Count:N0} with {dmMessages.Count:N0} messages");
        }

        private static async Task SeedPostVotesAsync(ApplicationDbContext ctx, List<Post> posts, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.PostVotes.AnyAsync()) return;

            var votes = new List<PostVote>();
            var seen = new HashSet<string>();

            foreach (var post in posts)
            {
                bool isViral = Rng.NextDouble() > 0.90;
                int voters = isViral
                    ? Rng.Next(100, Cfg.MaxVotersPerPost + 1)
                    : Rng.Next(0, 50);

                for (int i = 0; i < voters; i++)
                {
                    var user = Pick(users);
                    var key = $"{post.Id}|{user.Id}";
                    if (!seen.Add(key)) continue;

                    votes.Add(new PostVote
                    {
                        PostId = post.Id,
                        UserId = user.Id,
                        VoteValue = (short)(Rng.NextDouble() > 0.15 ? 1 : -1)
                    });
                }
            }
            await BatchInsertAsync(ctx, votes, "PostVotes");
        }

        private static async Task SeedPostViewsAsync(ApplicationDbContext ctx, List<Post> posts, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.PostViews.AnyAsync()) return;

            var views = new List<PostView>();
            var seen = new HashSet<string>();

            foreach (var post in posts)
            {
                bool isPopular = Rng.NextDouble() > 0.85;
                int viewers = isPopular
                    ? Rng.Next(200, Cfg.MaxViewersPerPost + 1)
                    : Rng.Next(5, 100);

                for (int i = 0; i < viewers; i++)
                {
                    var user = Pick(users);
                    var key = $"{post.Id}|{user.Id}";
                    if (!seen.Add(key)) continue;
                    views.Add(new PostView
                    {
                        Id = Guid.NewGuid(),
                        PostId = post.Id,
                        UserId = user.Id,
                        ViewedAt = post.CreatedAt.AddMinutes(Rng.Next(1, 40_000))
                    });
                }
            }
            await BatchInsertAsync(ctx, views, "PostViews");
        }

        private static async Task SeedMessageReactionsAsync(ApplicationDbContext ctx, List<Message> messages, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.MessageReactions.AnyAsync()) return;

            var reactions = new List<MessageReaction>();
            var seen = new HashSet<string>();

            int processed = 0;
            foreach (var msg in messages)
            {
                int count = Rng.NextDouble() > 0.7 ? Rng.Next(5, 20) : Rng.Next(0, 5);
                for (int i = 0; i < count; i++)
                {
                    var user = Pick(users);
                    var emoji = Pick(Emojis);
                    var key = $"{msg.Id}|{user.Id}|{emoji}";
                    if (!seen.Add(key)) continue;
                    reactions.Add(new MessageReaction { MessageId = msg.Id, UserId = user.Id, EmojiCode = emoji });
                }
                processed++;
                if (processed % 5000 == 0)
                    Console.Write($"\r[LoadSeeder]   MessageReactions: generating... {processed}/{messages.Count}");
            }
            Console.WriteLine();

            await BatchInsertAsync(ctx, reactions, "MessageReactions");
        }

        private static async Task SeedAuditLogsAsync(ApplicationDbContext ctx, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.AuditLogs.AnyAsync()) return;

            var admins = users.Where(u => u.Role == Role.Admin || u.Role == Role.Moderator).Take(100).ToList();
            var entities = new[] { "User", "Post", "Message", "Community", "Group", "Channel", "Report" };
            var logs = new List<AuditLog>();

            for (int i = 0; i < Cfg.AuditLogCount; i++)
            {
                logs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    ActorId = admins.Any() ? Pick(admins).Id : Pick(users).Id,
                    ActionType = Pick(AuditActions),
                    TargetEntity = Pick(entities),
                    TargetId = Guid.NewGuid(),
                    Changes = $"{{\"auto\":true,\"index\":{i}}}",
                    CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 365))
                });
            }
            await BatchInsertAsync(ctx, logs, "AuditLogs");
        }

        private static async Task SeedReportsAsync(ApplicationDbContext ctx, List<ApplicationUser> users, List<Post> posts, List<Message> messages, bool force = false)
        {
            if (!force && await ctx.Reports.AnyAsync()) return;

            var reasons = Enum.GetValues<ReportReason>();
            var reports = new List<Report>();

            for (int i = 0; i < Math.Min(Cfg.PostReportCount, posts.Count); i++)
            {
                var post = Pick(posts);
                var reporter = Pick(users);
                if (reporter.Id == post.AuthorId) continue;

                reports.Add(new Report
                {
                    Id = Guid.NewGuid(),
                    ReporterId = reporter.Id,
                    ReportedPostId = post.Id,
                    Reason = Pick(reasons),
                    Description = "Continut inadecvat raportat de utilizator.",
                    Status = Rng.NextDouble() < 0.55 ? ReportStatus.Pending : ReportStatus.Resolved,
                    CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 180))
                });
            }

            var sampleMessages = messages.Where(m => m.PostId != null).Take(500).ToList();
            foreach (var msg in sampleMessages)
            {
                var reporter = Pick(users);
                if (reporter.Id == msg.AuthorId) continue;

                reports.Add(new Report
                {
                    Id = Guid.NewGuid(),
                    ReporterId = reporter.Id,
                    ReportedMessageId = msg.Id,
                    Reason = Pick(reasons),
                    Description = "Mesaj raportat de utilizator.",
                    Status = Rng.NextDouble() < 0.6 ? ReportStatus.Pending : ReportStatus.Resolved,
                    CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 90))
                });
            }

            await BatchInsertAsync(ctx, reports, "Reports");
        }

        private static async Task SeedNotificationsAsync(ApplicationDbContext ctx, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.Notifications.AnyAsync()) return;

            var types = new[]
            {
                ("Bine ai venit!", "Exploreaza comunitatile si grupurile de studiu disponibile."),
                ("Raspuns nou", "Cineva a raspuns la postarea ta."),
                ("Vot primit", "Postarea ta a primit un vot pozitiv!"),
                ("Invitatie grup", "Ai fost invitat sa te alturi unui grup de studiu."),
                ("Mesaj nou", "Ai un mesaj direct nou."),
                ("Eveniment in curand", "Un eveniment la care esti inscris incepe maine."),
                ("Insigna obtinuta", "Felicitari! Ai obtinut o noua insigna de reputatie."),
                ("Postare populara", "Postarea ta a depasit 100 de vizualizari!"),
                ("Cerere de prietenie", "Ai primit o noua cerere de prietenie."),
                ("Prietenie acceptata", "Cererea ta de prietenie a fost acceptata!"),
            };

            var notifs = new List<Notification>();
            foreach (var user in users.Take(50_000))
            {
                int count = Rng.Next(3, Cfg.NotifPerUser + 5);
                for (int i = 0; i < count; i++)
                {
                    var (title, msg) = Pick(types);
                    notifs.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Title = title,
                        Message = msg,
                        IsRead = Rng.NextDouble() > 0.35,
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 90))
                    });
                }
            }
            await BatchInsertAsync(ctx, notifs, "Notifications");
        }

        private static async Task SeedLearningResourcesAsync(ApplicationDbContext ctx, List<ApplicationUser> users, List<StudyGroup> groups, List<StoredFile> files, bool force = false)
        {
            if (!force && await ctx.LearningResources.AnyAsync()) return;

            var resTypes = new[] { "PDF", "Video", "Link", "Article", "Exercise", "Slides", "Notebook" };
            var resources = new List<LearningResource>();

            foreach (var g in groups)
            {
                for (int i = 0; i < Cfg.ResourcesPerGroup; i++)
                {
                    resources.Add(new LearningResource
                    {
                        Id = Guid.NewGuid(),
                        GroupId = g.Id,
                        UploaderId = Pick(users).Id,
                        Title = $"Resursa {i + 1} — {g.SubjectArea ?? g.Name}",
                        Description = "Material util pentru pregatire si aprofundare a cunostintelor.",
                        ResourceType = Pick(resTypes),
                        Url = files.Any() && Rng.NextDouble() > 0.4 ? Pick(files).FilePath : $"https://example.com/resource/{Guid.NewGuid():N}",
                        DownloadCount = Rng.Next(0, 1500),
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 730))
                    });
                }
            }
            await BatchInsertAsync(ctx, resources, "LearningResources");
        }

        private static async Task SeedEventsAsync(ApplicationDbContext ctx, List<Community> communities, List<ApplicationUser> users, bool force = false)
        {
            if (!force && await ctx.Events.AnyAsync()) return;

            var templates = new[]
            {
                ("Sesiune Q&A Live", "Online", 1), ("Workshop Practic", "Sala C201", 3),
                ("Hackathon 24h", "Online", 24), ("Conferinta Tehnica", "Aula Magna", 8),
                ("Bootcamp Intensiv", "Online", 16), ("Code Review", "Online", 2),
                ("Meetup Comunitate", "Hub Inovare", 4), ("Prezentare Proiecte", "Sala A101", 3),
            };

            var events = new List<Event>();
            foreach (var comm in communities)
            {
                for (int i = 0; i < Cfg.EventsPerCommunity; i++)
                {
                    var (title, loc, hours) = Pick(templates);
                    var start = DateTime.UtcNow.AddDays(Rng.Next(-90, 120));
                    var maxAtt = Rng.Next(20, 500);
                    events.Add(new Event
                    {
                        Id = Guid.NewGuid(),
                        CommunityId = comm.Id,
                        OrganizerId = Pick(users).Id,
                        Title = $"{comm.Name} — {title}",
                        Description = $"Eveniment organizat pentru membrii comunitatii {comm.Name}.",
                        Location = loc,
                        StartTime = start,
                        EndTime = start.AddHours(hours),
                        MaxAttendees = maxAtt,
                        CurrentAttendees = Rng.Next(0, maxAtt),
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 90))
                    });
                }
            }
            await BatchInsertAsync(ctx, events, "Events");
        }

        private static async Task SeedGroupInvitesAsync(ApplicationDbContext ctx, List<StudyGroup> groups, bool force = false)
        {
            if (!force && await ctx.GroupInvites.AnyAsync()) return;

            var invites = new List<GroupInvite>();
            foreach (var g in groups)
            {
                for (int i = 0; i < Cfg.InvitesPerGroup; i++)
                {
                    invites.Add(new GroupInvite
                    {
                        Id = Guid.NewGuid(),
                        GroupId = g.Id,
                        CreatorId = g.OwnerId!,
                        Code = $"INV-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                        CreatedAt = DateTime.UtcNow.AddDays(-Rng.Next(1, 180)),
                        ExpiresAt = DateTime.UtcNow.AddDays(Rng.Next(7, 180)),
                        MaxUses = Rng.Next(5, 500),
                        CurrentUses = Rng.Next(0, 50)
                    });
                }
            }
            await BatchInsertAsync(ctx, invites, "GroupInvites");
        }

        private static void PrintSummary()
        {
            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║          ENTERPRISE LOAD-TEST SEED — COMPLETE                    ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════════╣");
            Console.WriteLine($"║  Users              up to {Cfg.UserCount,7:N0}                           ║");
            Console.WriteLine($"║  Communities                   10                               ║");
            Console.WriteLine($"║  Study Groups                  12                               ║");
            Console.WriteLine($"║  Posts/Community          {Cfg.PostsPerCommunity,6}                           ║");
            Console.WriteLine($"║  Posts/Channel            {Cfg.PostsPerChannel,6}                           ║");
            Console.WriteLine($"║  Comments/Post            {Cfg.CommentsPerPost,6}                           ║");
            Console.WriteLine($"║  Messages/Channel         ~{Cfg.ChannelMessagesPerChannel,6} (min)                      ║");
            Console.WriteLine($"║  Direct Conversations     {Cfg.DirectConvCount,6:N0}                           ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║  PASSWORD: Test@1234!  (works for all seeded accounts)           ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        }
    }
}