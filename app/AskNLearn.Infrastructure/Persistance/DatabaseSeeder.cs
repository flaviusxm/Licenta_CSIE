using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.Gamification;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.StudyGroup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Infrastructure.Persistance
{
    /// <summary>
    /// Enterprise-grade database seeder for AskNLearn.
    /// Seeding order respects all FK constraints:
    ///   Ranks → Tags → Users → StoredFiles → VerificationRequests
    ///   → Friendships → Communities → StudyGroups → GroupRoles
    ///   → GroupMemberships → ChannelCategories → Channels
    ///   → Posts → Messages → PostVotes → DirectConversations
    ///   → AuditLogs → Reports → Notifications → LearningResources → Events
    /// </summary>
    public static class DatabaseSeeder
    {
        // ─── Fixed test-account credentials (documented for QA) ─────────────
        public const string DefaultPassword = "Test@1234!";

        private static readonly (string Email, string FullName, Role Role, string Occupation, string Institution, bool IsVerified, int Rep)[] FixedAccounts =
        {
            ("admin@asknlearn.com",       "Admin System",          Role.Admin,     "System Administrator", "AskNLearn HQ",          true,  9999),
            ("moderator@asknlearn.com",   "Moderator General",     Role.Moderator, "Content Curator",      "AskNLearn HQ",          true,  5500),
            ("verified@asknlearn.com",    "Student Verificat",     Role.Member,    "Student",              "ASE Bucuresti",         true,  2200),
            ("member@asknlearn.com",      "Student Simplu",        Role.Member,    "Student",              "Politehnica Bucuresti", false, 350),
        };

        // ─── Randomisation helpers ───────────────────────────────────────────
        private static readonly Random Rng = new(42); // fixed seed → reproducible data

        private static readonly string[] FirstNames =
        {
            "Andrei","Maria","Alexandru","Elena","Stefan","Ioana","Mihai","Ana",
            "Cristian","Laura","Gabriel","Raluca","Ionut","Diana","Vlad","Andreea",
            "Matei","Simona","Claudiu","Monica","Bogdan","Teodora","Radu","Alina",
            "Cosmin","Bianca","Florin","Catalina","Sorin","Mirela"
        };
        private static readonly string[] LastNames =
        {
            "Popescu","Ionescu","Dumitrescu","Stan","Gheorghe","Rusu","Costin","Marin",
            "Tudor","Florescu","Nistor","Dobre","Barbu","Mihaila","Radulescu","Voinea",
            "Matei","Cristea","Diaconu","Enache","Popa","Constantin","Stoian","Neagu",
            "Badea","Iordache","Bucur","Vasilescu","Lazar","Grigore"
        };
        private static readonly string[] Domains =
            { "@yahoo.com", "@gmail.com", "@csie.ase.ro", "@stud.ase.ro", "@upb.ro", "@unibuc.ro" };
        private static readonly string[] Occupations =
            { "Student", "Master Student", "PhD Student", "Lecturer", "Researcher" };
        private static readonly string[] Institutions =
        {
            "ASE Bucuresti","Politehnica Bucuresti","Universitatea din Bucuresti",
            "SNSPA","Academia Navala","Universitatea Babes-Bolyai"
        };

        // ─── Rich content pools ──────────────────────────────────────────────
        private static readonly string[] PostTitles =
        {
            "Cum rezolv problema N+1 în Entity Framework?",
            "Best practices pentru autentificare JWT în .NET 8",
            "Diferenta dintre IQueryable si IEnumerable",
            "Index optimization în PostgreSQL - ghid complet",
            "CQRS vs Repository Pattern - ce alegi?",
            "Machine Learning cu Python - resurse pentru incepatori",
            "Proiect de licenta - alegere subiect AI sau Web?",
            "Cum scriu o lucrare academica buna?",
            "Microservices vs Monolith pentru proiecte universitare",
            "Git branching strategy pentru echipe mici",
            "Docker Compose pentru development local",
            "Algoritmul Dijkstra - explicatie pas cu pas",
            "Structuri de date: cand folosesti un AVL vs Red-Black Tree?",
            "SQL Window Functions - exemple practice",
            "Introducere in React pentru backend developers",
            "Securitate în aplicatii web - OWASP Top 10",
            "Clean Architecture in .NET - exemple reale",
            "Cum pregatesti un interviu tehnic la o firma IT?",
            "Resurse gratuite pentru invatarea Cloud AWS/Azure",
            "Cum optimizezi o aplicatie ASP.NET Core pentru productie?"
        };

        private static readonly string[] PostContents =
        {
            "Am intalnit aceasta problema in proiectul meu si nu stiu cum sa o rezolv. Orice ajutor este binevenit! Am incercat mai multe abordari dar fara succes.",
            "Dupa ce am studiat documentatia oficiala si mai multe tutoriale, am ajuns la concluzia ca cea mai buna abordare este urmatoarea...",
            "In cadrul cursului nostru, profesorul a mentionat aceasta tehnica dar nu a intrat in detalii. Cine poate sa explice mai pe larg?",
            "Am finalizat un proiect folosind aceasta tehnologie si pot sa impartasesc experienta mea. Iata ce am invatat pe parcurs...",
            "Cautam colegi pentru un proiect de grup la materia Baze de Date. Avem nevoie de 2-3 persoane cu cunostinte de SQL si C#.",
            "Am gasit un bug ciudat in codul meu si nu imi dau seama de unde vine. Stack trace-ul arata astfel: ...",
            "Care este parerea voastra despre aceasta biblioteca? Am citit recenzii mixte si nu stiu daca merita investit timp in ea.",
            "Nota importanta pentru toti colegii: examenul din saptamana viitoare va acoperi capitolele 5-8 din carte.",
            "Sharing resursele pe care le-am gasit utile pentru pregatirea examenului final. Sper sa va ajute si pe voi!",
            "Discutie deschisa: ce framework preferati pentru proiectele voastre personale si de ce?"
        };

        private static readonly string[] MessageContents =
        {
            "Super explicatie, multumesc!",
            "Am incercat si la mine merge. Mersi pentru indicatie!",
            "Poti sa dai si un exemplu de cod?",
            "Eu am intalnit aceeasi problema. Solutia mea a fost diferita.",
            "Nu sunt de acord complet, cred ca mai exista si alte aspecte.",
            "Excelent post! Il salvez pentru referinta.",
            "Am mai multe intrebari legate de asta, pot sa vin la consultatie?",
            "Link util: https://docs.microsoft.com",
            "Stiu un coleg care a lucrat pe asta, il contactez.",
            "Multumesc pentru raspuns, tocmai ce am rezolvat problema!",
            "Cred ca ar trebui sa cititi si RFC-ul aferent.",
            "Update: am gasit solutia, era o problema de configuratie.",
            "Cine altcineva a reusit? Eu tot am erori.",
            "Poate facem o sesiune de pair programming?",
            "Confirm, am testat pe PostgreSQL 15 si functioneaza.",
        };

        private static readonly string[] ChannelMessageContents =
        {
            "Salut tuturor! Prima prezenta in grup 👋",
            "Cand avem urmatoarea sedinta de studiu?",
            "Am incarcat niste resurse noi in sectiunea de fisiere.",
            "Reminder: maine avem deadline pentru proiect!",
            "Cineva poate sa explice notiunile din capitolul 3?",
            "Am rezolvat exercitiul 5, daca vrea cineva sa compare.",
            "Link catre codul sursa: https://github.com/example/repo",
            "Multumesc tuturor pentru ajutor la examen!",
            "Propun sa organizam o sesiune de recapitulare vineri.",
            "Am gasit un tutorial video excelent, vi-l recomand.",
            "Cine vine la sesiunea de voice chat de maine?",
            "Nota de la ultimul test: 9.50 🎉",
            "Am actualizat documentatia de pe wiki.",
            "Se poate pune si in pin mesajul cu deadline-urile?",
            "Succes tuturor la examene!",
        };

        // ─── Public entry point ──────────────────────────────────────────────

        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            Console.WriteLine("[Seeder] Starting enterprise seed (idempotent)...");
            try
            {
                // Layer 1 – static reference data
                var ranks       = await SeedRanksAsync(context);
                var tags        = await SeedTagsAsync(context);

                // Layer 2 – users (fixed QA accounts + 60 random)
                var users       = await SeedUsersAsync(userManager, context);

                // Layer 3 – user-owned entities
                var files       = await SeedStoredFilesAsync(context, users);
                await SeedVerificationRequestsAsync(context, users);
                await SeedFriendshipsAsync(context, users);

                // Layer 4 – communities & groups (no FK to posts/messages)
                var communities = await SeedCommunitiesAsync(context, users, tags);
                var groups      = await SeedStudyGroupsAsync(context, users);

                // Layer 5 – group infrastructure
                var (groupRoles, memberRoles) = await SeedGroupRolesAndMembershipsAsync(context, groups, users);
                await SeedChannelCategoriesAsync(context, groups);
                var channels    = await SeedChannelsAsync(context, groups);

                // Layer 6 – posts (FK to communities OR channels)
                var posts       = await SeedPostsAsync(context, communities, channels, users, tags);

                // Layer 7 – messages (FK to posts OR channels)
                await SeedMessagesAsync(context, posts, channels, users);
                var messages    = await context.Messages.AsNoTracking().ToListAsync();

                // Layer 8 – engagement (FK to posts & messages & users)
                await SeedPostVotesAsync(context, posts, users);
                await SeedPostViewsAsync(context, posts, users);
                await SeedMessageReactionsAsync(context, messages, users);

                // Layer 9 – direct messaging (FK to users only)
                await SeedDirectMessagingAsync(context, users);

                // Layer 10 – audit, reports, notifications
                await SeedAuditLogsAsync(context, users, posts);
                await SeedReportsAsync(context, users, posts, messages);
                await SeedNotificationsAsync(context, users);

                // Layer 11 – learning & events
                await SeedLearningResourcesAsync(context, users, groups, files);
                await SeedEventsAsync(context, communities, users);

                // Layer 12 – group invites
                await SeedGroupInvitesAsync(context, groups, users);

                Console.WriteLine("[Seeder] ✅ Seeding completed successfully.");
                PrintTestAccounts();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Seeder] ❌ FATAL ERROR:");
                PrintException(ex);
                throw;
            }
        }

        // ─── Layer 1: Ranks ──────────────────────────────────────────────────

        private static async Task<List<UserRank>> SeedRanksAsync(ApplicationDbContext context)
        {
            if (await context.UserRanks.AnyAsync())
                return await context.UserRanks.AsNoTracking().ToListAsync();

            var ranks = new List<UserRank>
            {
                new() { Name = "Novice",     MinPoints = 0,     IconUrl = "/icons/ranks/novice.png"     },
                new() { Name = "Apprentice", MinPoints = 500,   IconUrl = "/icons/ranks/apprentice.png" },
                new() { Name = "Scholar",    MinPoints = 1500,  IconUrl = "/icons/ranks/scholar.png"    },
                new() { Name = "Expert",     MinPoints = 3500,  IconUrl = "/icons/ranks/expert.png"     },
                new() { Name = "Master",     MinPoints = 7500,  IconUrl = "/icons/ranks/master.png"     },
                new() { Name = "Legend",     MinPoints = 15000, IconUrl = "/icons/ranks/legend.png"     },
            };
            await context.UserRanks.AddRangeAsync(ranks);
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] Ranks: {ranks.Count} seeded.");
            return ranks;
        }

        // ─── Layer 1: Tags ───────────────────────────────────────────────────

        private static async Task<List<Tag>> SeedTagsAsync(ApplicationDbContext context)
        {
            if (await context.Tags.AnyAsync())
                return await context.Tags.AsNoTracking().ToListAsync();

            var names = new[]
            {
                "Programming","C#","SQL","Machine Learning","Economics","Statistics",
                "Web Development","Database","AI","Cloud","Security","Finance",
                "Management","Marketing","Research","Algorithms","Data Structures",
                "DevOps","Mobile","UI/UX","Networking","Operating Systems",
                "Distributed Systems","Blockchain","Game Development"
            };
            var tags = names.Select(n => new Tag { Name = n }).ToList();
            await context.Tags.AddRangeAsync(tags);
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] Tags: {tags.Count} seeded.");
            return tags;
        }

        // ─── Layer 2: Users ──────────────────────────────────────────────────

        private static async Task<List<ApplicationUser>> SeedUsersAsync(
            UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            if (await context.Users.CountAsync() > 10)
                return await context.Users.AsNoTracking().ToListAsync();

            var users = new List<ApplicationUser>();

            // Fixed QA accounts
            foreach (var (email, name, role, occ, inst, verified, rep) in FixedAccounts)
            {
                var u = await CreateUserAsync(userManager, email, name, role, occ, inst, verified, rep, fixedDate: true);
                if (u != null) users.Add(u);
            }

            // 60 randomised members with varied reputation (covers all rank tiers)
            var repBuckets = new[] { 0, 200, 700, 1200, 2000, 4000, 8000, 16000 };
            for (int i = 1; i <= 60; i++)
            {
                var first = Pick(FirstNames);
                var last  = Pick(LastNames);
                var email = $"{first.ToLower()}.{last.ToLower()}{i}{Pick(Domains)}";
                var occ   = Pick(Occupations);
                var inst  = Pick(Institutions);
                var rep   = repBuckets[Rng.Next(repBuckets.Length)] + Rng.Next(0, 400);
                var verified = rep > 1000 && Rng.NextDouble() > 0.5;

                var u = await CreateUserAsync(userManager, email, $"{first} {last}", Role.Member, occ, inst, verified, rep);
                if (u != null) users.Add(u);
            }

            Console.WriteLine($"[Seeder] Users: {users.Count} seeded.");
            return await context.Users.AsNoTracking().ToListAsync();
        }

        private static async Task<ApplicationUser?> CreateUserAsync(
            UserManager<ApplicationUser> mgr,
            string email, string fullName, Role role,
            string occupation, string institution,
            bool isVerified, int rep,
            bool fixedDate = false)
        {
            var existing = await mgr.FindByEmailAsync(email);
            if (existing != null) return existing;

            var createdAt = fixedDate
                ? DateTime.UtcNow.AddYears(-1)
                : DateTime.SpecifyKind(DateTime.UtcNow.AddMonths(-Rng.Next(1, 24)), DateTimeKind.Utc);

            var user = new ApplicationUser
            {
                Id              = Guid.NewGuid().ToString(),
                UserName        = email,
                Email           = email,
                FullName        = fullName,
                EmailConfirmed  = true,
                Role            = role,
                IsVerified      = isVerified,
                ReputationPoints= rep,
                Bio             = $"{fullName} este {occupation} la {institution}. Pasionat de tehnologie si invatare continua.",
                Institution     = institution,
                Occupation      = occupation,
                AvatarUrl       = $"https://randomuser.me/api/portraits/{(Rng.Next(2) == 0 ? "men" : "women")}/{Rng.Next(1, 99)}.jpg",
                Status          = Pick(new[] { "Online", "Offline", "Away", "DND" }),
                SecurityStamp   = Guid.NewGuid().ToString(),
                CreatedAt       = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc),
            };

            var result = await mgr.CreateAsync(user, DefaultPassword);
            if (!result.Succeeded)
            {
                Console.WriteLine($"[Seeder] WARN – Could not create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return null;
            }
            return user;
        }

        // ─── Layer 3: StoredFiles ────────────────────────────────────────────

        private static async Task<List<StoredFile>> SeedStoredFilesAsync(
            ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.StoredFiles.AnyAsync())
                return await context.StoredFiles.AsNoTracking().ToListAsync();

            var extensions = new[] { "pdf", "docx", "xlsx", "pptx", "zip", "png", "jpg" };
            var mimes = new Dictionary<string, string>
            {
                ["pdf"]  = "application/pdf",
                ["docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ["xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ["pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ["zip"]  = "application/zip",
                ["png"]  = "image/png",
                ["jpg"]  = "image/jpeg",
            };

            var files = new List<StoredFile>();
            for (int i = 1; i <= 40; i++)
            {
                var ext    = Pick(extensions);
                var owner  = Pick(users);
                var file   = new StoredFile
                {
                    Id          = Guid.NewGuid(),
                    FileName    = $"document_{i:D3}.{ext}",
                    FilePath    = $"/uploads/files/{Guid.NewGuid():N}.{ext}",
                    FileType    = mimes[ext],
                    FileSize    = Rng.Next(512, 1024 * 1024 * 10),
                    UploaderId  = owner.Id,
                    UploadedAt  = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 365)), DateTimeKind.Utc),
                };
                files.Add(file);
            }
            await context.StoredFiles.AddRangeAsync(files);
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] StoredFiles: {files.Count} seeded.");
            return files;
        }

        // ─── Layer 3: VerificationRequests ──────────────────────────────────

        private static async Task SeedVerificationRequestsAsync(
            ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.VerificationRequests.AnyAsync()) return;

            var members = users.Where(u => u.Role == Role.Member).Take(20).ToList();
            var admins  = users.Where(u => u.Role == Role.Admin).ToList();
            var statuses = new[] { Status.Pending, Status.Approved, Status.Rejected };

            foreach (var student in members)
            {
                var status    = Pick(statuses);
                var submitted = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(5, 90)), DateTimeKind.Utc);
                var req = new VerificationRequest
                {
                    Id           = Guid.NewGuid(),
                    UserId       = student.Id,
                    StudentIdUrl = $"/uploads/verification/id_{Guid.NewGuid():N}.jpg",
                    CarnetUrl    = $"/uploads/verification/carnet_{Guid.NewGuid():N}.jpg",
                    Status       = status,
                    SubmittedAt  = submitted,
                };
                if (status != Status.Pending && admins.Any())
                {
                    req.ProcessedBy = Pick(admins).Id;
                    req.ProcessedAt = DateTime.SpecifyKind(submitted.AddDays(Rng.Next(1, 7)), DateTimeKind.Utc);
                }
                context.VerificationRequests.Add(req);
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] VerificationRequests: {members.Count} seeded.");
        }

        // ─── Layer 3: Friendships ────────────────────────────────────────────

        private static async Task SeedFriendshipsAsync(
            ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.Friendships.AnyAsync()) return;

            var seeded = new HashSet<(string, string)>();
            int count  = 0;

            foreach (var user in users.Take(40))
            {
                // Each user gets 3-8 friends
                var candidates = users
                    .Where(u => u.Id != user.Id)
                    .OrderBy(_ => Rng.Next())
                    .Take(Rng.Next(3, 9));

                foreach (var other in candidates)
                {
                    var pair = Ordered(user.Id, other.Id);
                    if (seeded.Contains(pair)) continue;
                    seeded.Add(pair);

                    var status = Rng.NextDouble() switch
                    {
                        < 0.70 => FriendshipStatus.Accepted,
                        < 0.85 => FriendshipStatus.Pending,
                        _      => FriendshipStatus.Blocked,
                    };

                    context.Friendships.Add(new Friendship
                    {
                        RequesterId = user.Id,
                        AddresseeId = other.Id,
                        Status      = status,
                        CreatedAt   = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 180)), DateTimeKind.Utc),
                    });
                    count++;
                }
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] Friendships: {count} seeded.");
        }

        // ─── Layer 4: Communities ────────────────────────────────────────────

        private static async Task<List<Community>> SeedCommunitiesAsync(
            ApplicationDbContext context, List<ApplicationUser> users, List<Tag> tags)
        {
            if (await context.Communities.AnyAsync())
                return await context.Communities.AsNoTracking().ToListAsync();

            var defs = new[]
            {
                ("Informatica Economica",      "ineconomica",       "Comunitate dedicata studentilor de la IE"),
                ("Cibernetica si Statistica",  "cibernetica",       "Matematica, statistica si modelare economica"),
                ("Inginerie Software",         "ing-software",      "Arhitecturi, design patterns, best practices"),
                ("Machine Learning & AI",      "ml-ai",             "Invatare automata si inteligenta artificiala"),
                ("Securitate Informatica",     "securitate",        "Cybersecurity, pentest, CTF challenges"),
                ("Web & Mobile Development",   "web-mobile",        "Frontend, backend, aplicatii mobile"),
            };

            var staff = users.Where(u => u.Role != Role.Member).ToList();
            if (!staff.Any()) staff = users.Take(2).ToList();

            var communities = new List<Community>();
            foreach (var (name, slug, desc) in defs)
            {
                var community = new Community
                {
                    Id          = Guid.NewGuid(),
                    Name        = name,
                    Slug        = slug,
                    Description = desc,
                    CreatorId   = Pick(staff).Id,
                    CreatedAt   = DateTime.SpecifyKind(DateTime.UtcNow.AddMonths(-Rng.Next(3, 18)), DateTimeKind.Utc),
                };
                context.Communities.Add(community);
                communities.Add(community);
            }
            await context.SaveChangesAsync();

            // Memberships: each user joins 1-4 communities
            var membershipSet = new HashSet<(Guid commId, string userId)>();
            foreach (var user in users)
            {
                var toJoin = communities.OrderBy(_ => Rng.Next()).Take(Rng.Next(1, 5));
                foreach (var comm in toJoin)
                {
                    if (!membershipSet.Add((comm.Id, user.Id))) continue;
                    var role = staff.Any(s => s.Id == user.Id) ? CommunityRole.Moderator : CommunityRole.Member;
                    context.CommunityMemberships.Add(new CommunityMembership
                    {
                        CommunityId = comm.Id,
                        UserId      = user.Id,
                        Role        = role,
                        JoinedAt    = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 300)), DateTimeKind.Utc),
                    });
                }
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] Communities: {communities.Count} seeded.");
            return communities;
        }

        // ─── Layer 4: StudyGroups ────────────────────────────────────────────

        private static async Task<List<StudyGroup>> SeedStudyGroupsAsync(
            ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.StudyGroups.AnyAsync())
                return await context.StudyGroups.AsNoTracking().ToListAsync();

            var defs = new[]
            {
                ("Pregatire Licenta 2025",   "Pregatire intensiva pentru examenul de licenta"),
                ("DotNet Advanced",          "Concepte avansate de C# si .NET 8: LINQ, async, DI"),
                ("C/C++ Algorithms",         "Competitive programming si algoritmi clasici"),
                ("SQL Mastery",              "Optimizare, indexare si tuning PostgreSQL/SQL Server"),
                ("DevOps & Cloud",           "Docker, Kubernetes, CI/CD, AWS, Azure"),
                ("React & TypeScript",       "Frontend modern cu React 18, hooks, state management"),
            };

            var staff = users.Where(u => u.Role != Role.Member).ToList();
            if (!staff.Any()) staff = users.Take(2).ToList();

            var groups = new List<StudyGroup>();
            foreach (var (name, desc) in defs)
            {
                var group = new StudyGroup
                {
                    Id          = Guid.NewGuid(),
                    Name        = name,
                    Description = desc,
                    OwnerId     = Pick(staff).Id,
                    CreatedAt   = DateTime.SpecifyKind(DateTime.UtcNow.AddMonths(-Rng.Next(1, 12)), DateTimeKind.Utc),
                };
                context.StudyGroups.Add(group);
                groups.Add(group);
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] StudyGroups: {groups.Count} seeded.");
            return groups;
        }

        // ─── Layer 5: GroupRoles, GroupMemberships, ChannelCategories ────────

        private static async Task<(List<GroupRole> adminRoles, List<GroupRole> memberRoles)> SeedGroupRolesAndMembershipsAsync(
            ApplicationDbContext context, List<StudyGroup> groups, List<ApplicationUser> users)
        {
            if (await context.GroupRoles.AnyAsync())
            {
                var existing = await context.GroupRoles.AsNoTracking().ToListAsync();
                return (existing.Where(r => r.Name == "Admin").ToList(),
                        existing.Where(r => r.Name == "Member").ToList());
            }

            var adminRoles  = new List<GroupRole>();
            var memberRoles = new List<GroupRole>();

            foreach (var group in groups)
            {
                var adminRole  = new GroupRole { Id = Guid.NewGuid(), GroupId = group.Id, Name = "Admin",     Permissions = "ALL" };
                var modRole    = new GroupRole { Id = Guid.NewGuid(), GroupId = group.Id, Name = "Moderator", Permissions = "MANAGE_MESSAGES,KICK" };
                var memberRole = new GroupRole { Id = Guid.NewGuid(), GroupId = group.Id, Name = "Member",    Permissions = "READ,WRITE" };

                context.GroupRoles.AddRange(adminRole, modRole, memberRole);
                adminRoles.Add(adminRole);
                memberRoles.Add(memberRole);
            }
            await context.SaveChangesAsync();

            // Memberships
            foreach (var group in groups)
            {
                var adminRole  = adminRoles.First(r => r.GroupId == group.Id);
                var memberRole = memberRoles.First(r => r.GroupId == group.Id);

                // Owner is admin
                if (group.OwnerId != null)
                {
                    context.GroupMemberships.Add(new GroupMembership
                    {
                        GroupId     = group.Id,
                        UserId      = group.OwnerId,
                        GroupRoleId = adminRole.Id,
                        JoinedAt    = DateTime.UtcNow,
                    });
                }

                // 10-20 random members
                var others = users
                    .Where(u => u.Id != group.OwnerId)
                    .OrderBy(_ => Rng.Next())
                    .Take(Rng.Next(10, 21));

                foreach (var user in others)
                {
                    context.GroupMemberships.Add(new GroupMembership
                    {
                        GroupId     = group.Id,
                        UserId      = user.Id,
                        GroupRoleId = memberRole.Id,
                        JoinedAt    = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 200)), DateTimeKind.Utc),
                    });
                }
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] GroupRoles & Memberships seeded.");
            return (adminRoles, memberRoles);
        }

        private static async Task SeedChannelCategoriesAsync(
            ApplicationDbContext context, List<StudyGroup> groups)
        {
            if (await context.ChannelCategories.AnyAsync()) return;

            foreach (var group in groups)
            {
                context.ChannelCategories.AddRange(
                    new ChannelCategory { Id = Guid.NewGuid(), GroupId = group.Id, Name = "General",   Position = 0 },
                    new ChannelCategory { Id = Guid.NewGuid(), GroupId = group.Id, Name = "Resurse",   Position = 1 },
                    new ChannelCategory { Id = Guid.NewGuid(), GroupId = group.Id, Name = "Proiecte",  Position = 2 }
                );
            }
            await context.SaveChangesAsync();
        }

        // ─── Layer 5: Channels ───────────────────────────────────────────────

        private static async Task<List<Channel>> SeedChannelsAsync(
            ApplicationDbContext context, List<StudyGroup> groups)
        {
            if (await context.Channels.AnyAsync())
                return await context.Channels.AsNoTracking().ToListAsync();

            var channels = new List<Channel>();
            foreach (var group in groups)
            {
                var toAdd = new[]
                {
                    new Channel { Id = Guid.NewGuid(), GroupId = group.Id, Name = "general",      Type = ChannelType.Text  },
                    new Channel { Id = Guid.NewGuid(), GroupId = group.Id, Name = "anunturi",     Type = ChannelType.Text  },
                    new Channel { Id = Guid.NewGuid(), GroupId = group.Id, Name = "off-topic",    Type = ChannelType.Text  },
                    new Channel { Id = Guid.NewGuid(), GroupId = group.Id, Name = "resurse",      Type = ChannelType.Text  },
                    new Channel { Id = Guid.NewGuid(), GroupId = group.Id, Name = "Voice Chat",   Type = ChannelType.Voice },
                    new Channel { Id = Guid.NewGuid(), GroupId = group.Id, Name = "Studiu Grup",  Type = ChannelType.Voice },
                };
                channels.AddRange(toAdd);
                context.Channels.AddRange(toAdd);
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] Channels: {channels.Count} seeded.");
            return channels;
        }

        // ─── Layer 6: Posts ──────────────────────────────────────────────────

        private static async Task<List<Post>> SeedPostsAsync(
            ApplicationDbContext context,
            List<Community> communities,
            List<Channel> channels,
            List<ApplicationUser> users,
            List<Tag> tags)
        {
            if (await context.Posts.AnyAsync())
                return await context.Posts.AsNoTracking().ToListAsync();

            var posts = new List<Post>();

            // Community posts: 8-15 per community
            foreach (var comm in communities)
            {
                int count = Rng.Next(8, 16);
                for (int i = 0; i < count; i++)
                {
                    var author  = Pick(users);
                    var created = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 200)), DateTimeKind.Utc);
                    var post    = new Post
                    {
                        Id          = Guid.NewGuid(),
                        CommunityId = comm.Id,
                        AuthorId    = author.Id,
                        Title       = Pick(PostTitles),
                        Content     = string.Join("\n\n", Enumerable.Range(0, Rng.Next(1, 4)).Select(_ => Pick(PostContents))),
                        CreatedAt   = created
                    };
                    context.Posts.Add(post);
                    posts.Add(post);

                    // Attach 1-3 tags
                    var postTags = tags.OrderBy(_ => Rng.Next()).Take(Rng.Next(1, 4))
                        .Select(t => new PostTag { PostId = post.Id, Post = post, TagId = t.Id, Tag = t });
                    context.PostTags.AddRange(postTags);
                }
            }

            // Channel discussion posts: 4-8 per text channel
            foreach (var chan in channels.Where(c => c.Type == ChannelType.Text))
            {
                int count = Rng.Next(4, 9);
                for (int i = 0; i < count; i++)
                {
                    var post = new Post
                    {
                        Id        = Guid.NewGuid(),
                        ChannelId = chan.Id,
                        AuthorId  = Pick(users).Id,
                        Title     = $"Topic: {Pick(PostTitles)}",
                        Content   = Pick(PostContents),
                        CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 100)), DateTimeKind.Utc),
                    };
                    context.Posts.Add(post);
                    posts.Add(post);
                }
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] Posts: {posts.Count} seeded.");
            return posts;
        }

        // ─── Layer 7: Messages ───────────────────────────────────────────────

        private static async Task SeedMessagesAsync(
            ApplicationDbContext context,
            List<Post> posts,
            List<Channel> channels,
            List<ApplicationUser> users)
        {
            if (await context.Messages.AnyAsync()) return;

            int total = 0;

            // Post replies: 2-8 per post
            foreach (var post in posts)
            {
                int count = Rng.Next(2, 9);
                Message? previous = null;
                for (int i = 0; i < count; i++)
                {
                    var msg = new Message
                    {
                        Id        = Guid.NewGuid(),
                        PostId    = post.Id,
                        AuthorId  = Pick(users).Id,
                        Content   = Pick(MessageContents),
                        CreatedAt = DateTime.SpecifyKind(post.CreatedAt.AddMinutes(Rng.Next(5, 10000)), DateTimeKind.Utc),
                        ReplyToMessageId = (previous != null && Rng.NextDouble() > 0.6) ? previous.Id : null,
                    };
                    context.Messages.Add(msg);
                    previous = msg;
                    total++;
                }
            }

            // Channel messages: 15-30 per text channel
            foreach (var chan in channels.Where(c => c.Type == ChannelType.Text))
            {
                int count = Rng.Next(15, 31);
                for (int i = 0; i < count; i++)
                {
                    context.Messages.Add(new Message
                    {
                        Id        = Guid.NewGuid(),
                        ChannelId = chan.Id,
                        AuthorId  = Pick(users).Id,
                        Content   = Pick(ChannelMessageContents),
                        CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-Rng.Next(1, 2000)), DateTimeKind.Utc),
                    });
                    total++;
                }
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] Messages: {total} seeded.");
        }

        // ─── Layer 8: Engagement ─────────────────────────────────────────────

        private static async Task SeedPostVotesAsync(
            ApplicationDbContext context, List<Post> posts, List<ApplicationUser> users)
        {
            if (await context.PostVotes.AnyAsync()) return;

            var seen  = new HashSet<(Guid postId, string userId)>();
            int count = 0;

            foreach (var post in posts)
            {
                int voters = Rng.Next(0, Math.Min(users.Count, 20));
                foreach (var voter in users.OrderBy(_ => Rng.Next()).Take(voters))
                {
                    if (!seen.Add((post.Id, voter.Id))) continue;
                    context.PostVotes.Add(new PostVote
                    {
                        PostId    = post.Id,
                        UserId    = voter.Id,
                        VoteValue = (short)(Rng.NextDouble() > 0.25 ? 1 : -1), // 75% upvotes
                    });
                    count++;
                }
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] PostVotes: {count} seeded.");
        }

        private static async Task SeedPostViewsAsync(
            ApplicationDbContext context, List<Post> posts, List<ApplicationUser> users)
        {
            if (await context.PostViews.AnyAsync()) return;

            var seen  = new HashSet<(Guid postId, string userId)>();
            int count = 0;

            foreach (var post in posts)
            {
                int viewers = Rng.Next(1, Math.Min(users.Count, 30));
                foreach (var viewer in users.OrderBy(_ => Rng.Next()).Take(viewers))
                {
                    if (!seen.Add((post.Id, viewer.Id))) continue;
                    context.PostViews.Add(new PostView
                    {
                        PostId   = post.Id,
                        UserId   = viewer.Id,
                        ViewedAt = DateTime.SpecifyKind(post.CreatedAt.AddMinutes(Rng.Next(1, 5000)), DateTimeKind.Utc),
                    });
                    count++;
                }
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] PostViews: {count} seeded.");
        }

        private static async Task SeedMessageReactionsAsync(
            ApplicationDbContext context, List<Message> messages, List<ApplicationUser> users)
        {
            if (await context.MessageReactions.AnyAsync()) return;

            var emojis = new[] { "👍", "❤️", "😂", "🎉", "🤔", "👀", "🔥", "✅" };
            var seen   = new HashSet<(Guid msgId, string userId, string emoji)>();
            int count  = 0;

            foreach (var msg in messages.OrderBy(_ => Rng.Next()).Take(messages.Count / 2))
            {
                int reactors = Rng.Next(0, 6);
                foreach (var user in users.OrderBy(_ => Rng.Next()).Take(reactors))
                {
                    var emoji = Pick(emojis);
                    if (!seen.Add((msg.Id, user.Id, emoji))) continue;
                    context.MessageReactions.Add(new MessageReaction
                    {
                        MessageId = msg.Id,
                        UserId    = user.Id,
                        EmojiCode = emoji,
                    });
                    count++;
                }
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] MessageReactions: {count} seeded.");
        }

        // ─── Layer 9: DirectMessaging ────────────────────────────────────────

        private static async Task SeedDirectMessagingAsync(
            ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.DirectConversations.AnyAsync()) return;

            var seeded = new HashSet<(string, string)>();
            int convCount = 0;

            for (int i = 0; i < 20; i++)
            {
                var u1   = Pick(users);
                var u2   = users.Where(u => u.Id != u1.Id).OrderBy(_ => Rng.Next()).First();
                var pair = Ordered(u1.Id, u2.Id);
                if (seeded.Contains(pair)) continue;
                seeded.Add(pair);

                var conv = new DirectConversation
                {
                    Id        = Guid.NewGuid(),
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 60)), DateTimeKind.Utc),
                };
                context.DirectConversations.Add(conv);
                await context.SaveChangesAsync();

                context.DirectConversationParticipants.AddRange(
                    new DirectConversationParticipant { ConversationId = conv.Id, UserId = u1.Id },
                    new DirectConversationParticipant { ConversationId = conv.Id, UserId = u2.Id }
                );
                await context.SaveChangesAsync();
                convCount++;
            }
            Console.WriteLine($"[Seeder] DirectConversations: {convCount} seeded.");
        }

        // ─── Layer 10: Audit, Reports, Notifications ─────────────────────────

        private static async Task SeedAuditLogsAsync(
            ApplicationDbContext context, List<ApplicationUser> users, List<Post> posts)
        {
            if (await context.AuditLogs.AnyAsync()) return;

            var actions = new[] { "USER_CREATED", "POST_DELETED", "USER_BANNED", "ROLE_CHANGED", "REPORT_RESOLVED", "CONTENT_FLAGGED" };
            var admins  = users.Where(u => u.Role == Role.Admin || u.Role == Role.Moderator).ToList();
            if (!admins.Any()) admins = users.Take(2).ToList();

            for (int i = 0; i < 30; i++)
            {
                context.AuditLogs.Add(new AuditLog
                {
                    Id         = Guid.NewGuid(),
                    ActorId    = Pick(admins).Id,
                    ActionType = Pick(actions),
                    Changes    = $"Actiune efectuata la {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    CreatedAt  = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 90)), DateTimeKind.Utc),
                });
            }
            await context.SaveChangesAsync();
            Console.WriteLine("[Seeder] AuditLogs: 30 seeded.");
        }

        private static async Task SeedReportsAsync(
            ApplicationDbContext context,
            List<ApplicationUser> users,
            List<Post> posts,
            List<Message> messages)
        {
            if (await context.Reports.AnyAsync()) return;

            var reasons  = Enum.GetValues<ReportReason>();
            var statuses = Enum.GetValues<ReportStatus>();
            int count    = 0;

            // Post reports
            foreach (var post in posts.OrderBy(_ => Rng.Next()).Take(10))
            {
                var reporter = Pick(users.Where(u => u.Id != post.AuthorId).ToList());
                context.Reports.Add(new Report
                {
                    Id             = Guid.NewGuid(),
                    ReporterId     = reporter.Id,
                    ReportedPostId = post.Id,
                    Reason         = Pick(reasons),
                    Description    = "Continut raportat de utilizator.",
                    Status         = Pick(statuses),
                    CreatedAt      = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 60)), DateTimeKind.Utc),
                });
                count++;
            }

            // Message reports
            foreach (var msg in messages.OrderBy(_ => Rng.Next()).Take(5))
            {
                var reporter = Pick(users.Where(u => u.Id != msg.AuthorId).ToList());
                context.Reports.Add(new Report
                {
                    Id                = Guid.NewGuid(),
                    ReporterId        = reporter.Id,
                    ReportedMessageId = msg.Id,
                    Reason            = Pick(reasons),
                    Description       = "Mesaj raportat.",
                    Status            = Pick(statuses),
                    CreatedAt         = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 60)), DateTimeKind.Utc),
                });
                count++;
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] Reports: {count} seeded.");
        }

        private static async Task SeedNotificationsAsync(
            ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.Notifications.AnyAsync()) return;

            var types = new[]
            {
                ("Bine ai venit!",            "Exploreaza comunitatile si grupurile de studiu."),
                ("Raspuns nou",               "Cineva a raspuns la postarea ta."),
                ("Vot primit",                "Postarea ta a primit un vot pozitiv."),
                ("Cerere de prietenie",       "Ai primit o cerere de prietenie."),
                ("Invitatie grup",            "Ai fost invitat intr-un grup de studiu."),
                ("Eveniment in curand",       "Un eveniment din comunitatea ta incepe in 24h."),
                ("Resursa noua",              "A fost adaugata o resursa noua in grupul tau."),
                ("Verificare aprobata",       "Contul tau a fost verificat cu succes."),
            };

            int count = 0;
            foreach (var user in users)
            {
                int notifCount = Rng.Next(1, 6);
                for (int i = 0; i < notifCount; i++)
                {
                    var (title, msg) = Pick(types);
                    context.Notifications.Add(new Notification
                    {
                        Id        = Guid.NewGuid(),
                        UserId    = user.Id,
                        Title     = title,
                        Message   = msg,
                        IsRead    = Rng.NextDouble() > 0.4,
                        CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 30)), DateTimeKind.Utc),
                    });
                    count++;
                }
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] Notifications: {count} seeded.");
        }

        // ─── Layer 11: LearningResources & Events ────────────────────────────

        private static async Task SeedLearningResourcesAsync(
            ApplicationDbContext context,
            List<ApplicationUser> users,
            List<StudyGroup> groups,
            List<StoredFile> files)
        {
            if (await context.LearningResources.AnyAsync()) return;

            var resourceTypes = new[] { "PDF", "Video", "Link", "Article", "Exercise", "Cheatsheet" };
            int count = 0;

            foreach (var group in groups)
            {
                int resCount = Rng.Next(3, 8);
                for (int i = 0; i < resCount; i++)
                {
                    var file = files.Count > 0 && Rng.NextDouble() > 0.5 ? Pick(files) : null;
                    context.LearningResources.Add(new LearningResource
                    {
                        Id           = Guid.NewGuid(),
                        GroupId      = group.Id,
                        UploaderId   = Pick(users).Id,
                        Title        = $"Resursa {i + 1}: {Pick(new[] { "Ghid", "Tutorial", "Exercitii", "Note de curs", "Cheatsheet" })}",
                        Description  = "Material util pentru pregatire si aprofundare.",
                        ResourceType = Pick(resourceTypes),
                        Url          = file?.FilePath ?? $"https://example.com/resource/{Guid.NewGuid():N}",
                        CreatedAt    = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 180)), DateTimeKind.Utc),
                    });
                    count++;
                }
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] LearningResources: {count} seeded.");
        }

        private static async Task SeedEventsAsync(
            ApplicationDbContext context,
            List<Community> communities,
            List<ApplicationUser> users)
        {
            if (await context.Events.AnyAsync()) return;

            var eventTemplates = new[]
            {
                ("Sesiune Q&A Live",       "Online (Microsoft Teams)",  1),
                ("Workshop Practic",       "Sala C201, ASE Bucuresti",  3),
                ("Hackathon 24h",          "Online (Discord)",          24),
                ("Prezentare Proiecte",    "Amfiteatru Moxa",           2),
                ("Meetup Lunar",           "Hub-ul Antreprenorial",     2),
                ("Concurs Algoritmi",      "Online (Codeforces)",       4),
            };

            int count = 0;
            foreach (var comm in communities)
            {
                int numEvents = Rng.Next(2, 5);
                for (int i = 0; i < numEvents; i++)
                {
                    var (title, location, hours) = Pick(eventTemplates);
                    var start = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(Rng.Next(-10, 30)), DateTimeKind.Utc);
                    context.Events.Add(new Event
                    {
                        Id          = Guid.NewGuid(),
                        CommunityId = comm.Id,
                        OrganizerId = Pick(users).Id,
                        Title       = $"{comm.Name} – {title}",
                        Description = $"Eveniment organizat pentru membrii comunitatii {comm.Name}.",
                        Location    = location,
                        StartTime   = start,
                        EndTime     = start.AddHours(hours),
                        CreatedAt   = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 20)), DateTimeKind.Utc),
                    });
                    count++;
                }
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] Events: {count} seeded.");
        }

        // ─── Layer 12: GroupInvites ───────────────────────────────────────────

        private static async Task SeedGroupInvitesAsync(
            ApplicationDbContext context,
            List<StudyGroup> groups,
            List<ApplicationUser> users)
        {
            if (await context.GroupInvites.AnyAsync()) return;

            int count = 0;
            foreach (var group in groups)
            {
                var invitees = users.OrderBy(_ => Rng.Next()).Take(Rng.Next(2, 6));
                foreach (var invitee in invitees)
                {
                    context.GroupInvites.Add(new GroupInvite
                    {
                        Id          = Guid.NewGuid(),
                        GroupId     = group.Id,
                        CreatorId   = group.OwnerId!,
                        Code        = $"INV-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}",
                        CreatedAt   = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-Rng.Next(1, 60)), DateTimeKind.Utc),
                        ExpiresAt   = DateTime.UtcNow.AddDays(30),
                        MaxUses     = 10,
                        CurrentUses = Rng.Next(0, 5)
                    });
                    count++;
                }
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seeder] GroupInvites: {count} seeded.");
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static T Pick<T>(IList<T> list) => list[Rng.Next(list.Count)];
        private static T Pick<T>(T[] arr)        => arr[Rng.Next(arr.Length)];

        private static (string, string) Ordered(string a, string b) =>
            string.Compare(a, b, StringComparison.Ordinal) < 0 ? (a, b) : (b, a);

        private static void PrintTestAccounts()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              TEST ACCOUNTS (password: Test@1234!)            ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║  ADMIN     │ admin@asknlearn.com                             ║");
            Console.WriteLine("║  MODERATOR │ moderator@asknlearn.com                         ║");
            Console.WriteLine("║  VERIFIED  │ verified@asknlearn.com                          ║");
            Console.WriteLine("║  MEMBER    │ member@asknlearn.com                            ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");
        }

        private static void PrintException(Exception ex)
        {
            var cur = ex;
            while (cur != null)
            {
                Console.WriteLine($"  [{cur.GetType().Name}] {cur.Message}");
                cur = cur.InnerException;
            }
        }
    }
}