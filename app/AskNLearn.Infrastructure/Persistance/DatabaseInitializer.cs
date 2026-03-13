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
using System.Text.Json;
using System.Threading.Tasks;

namespace AskNLearn.Infrastructure.Persistance
{
    public static class DatabaseInitializer
    {
        private static readonly Random _random = new Random(42); // Fixed seed for reproducibility
        private static readonly string[] _firstNames = { "Andrei", "Maria", "Alexandru", "Elena", "Mihai", "Ioana", "Stefan", "Cristina", "Vlad", "Diana", "Radu", "Ana", "Gabriel", "Roxana", "Tudor", "Oana", "Marius", "Laura", "Sorin", "Alina" };
        private static readonly string[] _lastNames = { "Popescu", "Ionescu", "Stan", "Dumitrescu", "Gheorghe", "Radulescu", "Marin", "Diaconu", "Nistor", "Florescu", "Barbu", "Voicu", "Munteanu", "Cristea", "Mihaila", "Constantin", "Dobre", "Tudor", "Petrescu", "Anghel" };
        private static readonly string[] _domains = { "@gmail.com", "@yahoo.com", "@stud.ase.ro", "@csie.ase.ro", "@upb.ro", "@unibuc.ro" };

        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            Console.WriteLine("Starting database seeding...");

            // Seed core data first
            await SeedRanksAsync(context);
            await SeedTagsAsync(context);

            // Seed users (expanded to 50+ users)
            var users = await SeedUsersAsync(userManager, context);

            // Seed files and verification
            await SeedStoredFilesAsync(context, users);
            await SeedVerificationRequestsAsync(context, users);

            // Social connections
            await SeedFriendshipsAsync(context, users);

            // Communities and content
            var communities = await SeedCommunitiesAsync(context, users);
            var studyGroups = await SeedStudyGroupsAsync(context, users);
            await SeedGroupAddonsAsync(context, studyGroups, users);
            var channels = await SeedChannelsAsync(context, studyGroups);

            // Posts and discussions
            var posts = await SeedPostsAsync(context, communities, channels, users);

            // Messaging system
            await SeedMessagesAsync(context, posts, channels, users);
            var messages = await context.Messages.ToListAsync();

            // Engagement metrics
            await SeedEngagementAsync(context, posts, messages, users);
            await SeedDirectMessagingAsync(context, users);

            // System data
            await SeedAuditAndReportsAsync(context, posts, messages, users);
            await SeedNotificationsAsync(context, users);

            // Learning resources
            await SeedLearningResourcesAsync(context, users, studyGroups);

            // Events and announcements
            await SeedEventsAsync(context, communities, users);

            await context.SaveChangesAsync();
            Console.WriteLine($"Seeding complete. Users: {users.Count}, Communities: {communities.Count}, Posts: {posts.Count}");
        }

        private static async Task SeedRanksAsync(ApplicationDbContext context)
        {
            if (await context.UserRanks.AnyAsync()) return;

            var ranks = new[]
            {
                new UserRank { Id = Guid.NewGuid(), Name = "Novice", MinPoints = 0, IconUrl = "/icons/ranks/novice.png" },
                new UserRank { Id = Guid.NewGuid(), Name = "Scholar", MinPoints = 500, IconUrl = "/icons/ranks/scholar.png" },
                new UserRank { Id = Guid.NewGuid(), Name = "Expert", MinPoints = 1200, IconUrl = "/icons/ranks/expert.png" },
                new UserRank { Id = Guid.NewGuid(), Name = "Guru", MinPoints = 3000, IconUrl = "/icons/ranks/guru.png" },
                new UserRank { Id = Guid.NewGuid(), Name = "Legend", MinPoints = 7500, IconUrl = "/icons/ranks/legend.png" },
                new UserRank { Id = Guid.NewGuid(), Name = "Grandmaster", MinPoints = 15000, IconUrl = "/icons/ranks/grandmaster.png" }
            };

            await context.UserRanks.AddRangeAsync(ranks);
            await context.SaveChangesAsync();
        }

        private static async Task SeedTagsAsync(ApplicationDbContext context)
        {
            if (await context.Tags.AnyAsync()) return;

            var tagNames = new[]
            {
                "C#", "Java", "Python", "JavaScript", "React", "Angular", "Vue", "Node.js",
                "Database", "SQL", "NoSQL", "MongoDB", "PostgreSQL", "MySQL",
                "AI", "Machine Learning", "Deep Learning", "Data Science", "Big Data",
                "Algorithms", "Data Structures", "Design Patterns", "Clean Code",
                "Calculus", "Linear Algebra", "Statistics", "Probability",
                "Microeconomics", "Macroeconomics", "Econometrics", "Finance",
                "Accounting", "Marketing", "Management", "Business Strategy",
                "Cyber Security", "Networking", "Cloud Computing", "DevOps"
            };

            foreach (var name in tagNames)
            {
                context.Tags.Add(new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    UsageCount = _random.Next(10, 500)
                });
            }

            await context.SaveChangesAsync();
        }

        private static async Task<List<ApplicationUser>> SeedUsersAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            var users = new List<ApplicationUser>();

            // Create admin users
            var admins = new[]
            {
                ("admin@asknlearn.com", "Dr. Sarah Chen", Role.Admin, "Lead Architect", "MIT", true, 15200),
                ("marcus.r@asknlearn.com", "Marcus Rodriguez", Role.Admin, "Community Manager", "Stanford", true, 8750),
                ("elena.p@asknlearn.com", "Prof. Elena Popescu", Role.Admin, "Department Head", "ASE Bucuresti", true, 12400)
            };

            foreach (var (email, name, role, occupation, institution, verified, rep) in admins)
            {
                var user = await CreateUserAsync(userManager, email, name, role, occupation, institution, verified, rep);
                if (user != null) users.Add(user);
            }

            // Create moderator users
            var moderators = new[]
            {
                ("emily.watson@example.com", "Emily Watson", Role.Moderator, "PhD Candidate", "Stanford", true, 5200),
                ("james.kim@example.com", "James Kim", Role.Moderator, "Senior Developer", "Google", true, 4800),
                ("prof.andrei@ase.ro", "Prof. Andrei Popescu", Role.Moderator, "Professor", "ASE Bucuresti", true, 6300),
                ("prof.elena@ase.ro", "Prof. Elena Ionescu", Role.Moderator, "Professor", "ASE Bucuresti", true, 5900),
                ("mihai.constantin@upb.ro", "Mihai Constantin", Role.Moderator, "Teaching Assistant", "UPB", true, 4100),
                ("cristina.munteanu@unibuc.ro", "Cristina Munteanu", Role.Moderator, "Researcher", "UniBuc", true, 4450)
            };

            foreach (var (email, name, role, occupation, institution, verified, rep) in moderators)
            {
                var user = await CreateUserAsync(userManager, email, name, role, occupation, institution, verified, rep);
                if (user != null) users.Add(user);
            }

            // Create 50+ member users
            for (int i = 1; i <= 50; i++)
            {
                var firstName = _firstNames[_random.Next(_firstNames.Length)];
                var lastName = _lastNames[_random.Next(_lastNames.Length)];
                var fullName = $"{firstName} {lastName}";
                var domain = _domains[_random.Next(_domains.Length)];
                var email = $"{firstName.ToLower()}.{lastName.ToLower()}{i}{domain}";

                var occupation = _random.Next(3) switch
                {
                    0 => "Student",
                    1 => "Master Student",
                    _ => "PhD Student"
                };

                var institution = _random.Next(4) switch
                {
                    0 => "ASE Bucuresti",
                    1 => "Politehnica Bucuresti",
                    2 => "Universitatea din Bucuresti",
                    _ => "SNSPA"
                };

                var repPoints = _random.Next(0, 3500);
                var isVerified = repPoints > 1000 && _random.NextDouble() > 0.7;

                var user = await CreateUserAsync(userManager, email, fullName, Role.Member, occupation, institution, isVerified, repPoints);
                if (user != null) users.Add(user);
            }

            // Add some inactive users
            for (int i = 1; i <= 10; i++)
            {
                var firstName = _firstNames[_random.Next(_firstNames.Length)];
                var lastName = _lastNames[_random.Next(_lastNames.Length)];
                var email = $"{firstName.ToLower()}.{lastName.ToLower()}.inactive{i}@example.com";

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = $"{firstName} {lastName}",
                    EmailConfirmed = true,
                    Role = Role.Member,
                    IsVerified = false,
                    ReputationPoints = _random.Next(0, 200),
                    Bio = $"Inactive user account",
                    CreatedAt = DateTime.UtcNow.AddMonths(-_random.Next(6, 18)),
                    LastActive = DateTime.UtcNow.AddMonths(-_random.Next(2, 6)),
                    Status = UserStatus.Offline.ToString()
                };

                var result = await userManager.CreateAsync(user, "TestPassword123!");
                if (result.Succeeded) users.Add(user);
            }

            await context.SaveChangesAsync();
            return users;
        }

        private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager,
            string email, string fullName, Role role, string occupation, string institution, bool isVerified, int repPoints)
        {
            if (await userManager.FindByEmailAsync(email) != null)
                return null;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                Role = role,
                IsVerified = isVerified,
                ReputationPoints = repPoints,
                Bio = GenerateBio(fullName, occupation, institution),
                Institution = institution,
                Occupation = occupation,
                Interests = GenerateInterests(),
                AvatarUrl = $"https://randomuser.me/api/portraits/{(_random.Next(2) == 0 ? "men" : "women")}/{_random.Next(1, 99)}.jpg",
                BannerUrl = $"https://picsum.photos/id/{_random.Next(1, 200)}/1500/500",
                SocialLinks = JsonSerializer.Serialize(new
                {
                    linkedin = $"https://linkedin.com/in/{email.Split('@')[0]}",
                    github = $"https://github.com/{email.Split('@')[0]}",
                    twitter = $"https://twitter.com/@{email.Split('@')[0]}"
                }),
                Status = _random.Next(4) switch
                {
                    0 => UserStatus.Online.ToString(),
                    1 => UserStatus.Away.ToString(),
                    2 => UserStatus.DoNotDisturb.ToString(),
                    _ => UserStatus.Offline.ToString()
                },
                LastActive = DateTime.UtcNow.AddMinutes(-_random.Next(0, 1440)),
                CreatedAt = DateTime.UtcNow.AddMonths(-_random.Next(1, 24))
            };

            var result = await userManager.CreateAsync(user, "TestPassword123!");
            return result.Succeeded ? user : null;
        }

        private static string GenerateBio(string fullName, string occupation, string institution)
        {
            var bios = new[]
            {
                $"{fullName} is a passionate {occupation} at {institution}. Interested in technology and education.",
                $"{occupation} at {institution} with a focus on research and innovation. Always eager to learn and share knowledge.",
                $"Currently studying at {institution} and exploring the intersection of computer science and economics.",
                $"{occupation} specializing in data science and machine learning. Love helping others understand complex concepts.",
                $"Dedicated {occupation} at {institution}. Building the future of education through technology.",
                $"Research assistant and {occupation} at {institution}. Passionate about open source and collaborative learning."
            };

            return bios[_random.Next(bios.Length)];
        }

        private static string GenerateInterests()
        {
            var interestPools = new[]
            {
                "Machine Learning, Artificial Intelligence, Data Science",
                "Web Development, Cloud Computing, DevOps",
                "Mobile Development, UI/UX Design, Product Management",
                "Database Design, System Architecture, Performance Optimization",
                "Economics, Finance, Business Strategy",
                "Statistics, Mathematics, Quantitative Analysis",
                "Cyber Security, Network Protocols, Cryptography",
                "Game Development, Computer Graphics, VR/AR",
                "Open Source, Community Building, Tech Education",
                "Research, Academic Writing, Scientific Computing"
            };

            return interestPools[_random.Next(interestPools.Length)];
        }

        private static async Task SeedStoredFilesAsync(ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.StoredFiles.AnyAsync()) return;

            var fileTypes = new[] { "application/pdf", "image/png", "image/jpeg", "application/zip", "text/plain" };
            var fileNames = new[] { "lecture-notes", "assignment", "presentation", "dataset", "code-sample", "research-paper", "tutorial", "cheat-sheet" };
            var modules = new[] { "Community", "Group", "Profile", "Post", "Message" };

            for (int i = 1; i <= 100; i++)
            {
                var uploader = users[_random.Next(users.Count)];
                var fileType = fileTypes[_random.Next(fileTypes.Length)];
                var extension = fileType.Split('/')[1];
                var fileName = $"{fileNames[_random.Next(fileNames.Length)]}_{i}.{extension}";

                context.StoredFiles.Add(new StoredFile
                {
                    Id = Guid.NewGuid(),
                    UploaderId = uploader.Id,
                    FileName = fileName,
                    FilePath = $"/uploads/{DateTime.UtcNow.Year}/{DateTime.UtcNow.Month:D2}/{fileName}",
                    FileType = fileType,
                    FileSize = _random.Next(10240, 50 * 1024 * 1024), // 10KB to 50MB
                    ModuleContext = modules[_random.Next(modules.Length)],
                    UploadedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 90))
                });
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedVerificationRequestsAsync(ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.VerificationRequests.AnyAsync()) return;

            var unverifiedStudents = users.Where(u => !u.IsVerified && u.Role == Role.Member).Take(25).ToList();
            var admins = users.Where(u => u.Role == Role.Admin).ToList();

            foreach (var student in unverifiedStudents)
            {
                var status = _random.Next(10) switch
                {
                    0 => Status.Pending,
                    1 => Status.Rejected,
                    _ => Status.Approved
                };

                var request = new VerificationRequest
                {
                    Id = Guid.NewGuid(),
                    UserId = student.Id,
                    StudentIdUrl = $"/uploads/verification/student_id_{student.UserName}.jpg",
                    CarnetUrl = $"/uploads/verification/carnet_{student.UserName}.jpg",
                    Status = status,
                    SubmittedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 60)),
                    AdminNotes = status == Status.Rejected ? "Please provide a clearer image of your student ID" : null
                };

                if (status != Status.Pending && admins.Any())
                {
                    request.ProcessedBy = admins[_random.Next(admins.Count)].Id;
                    request.ProcessedAt = request.SubmittedAt.AddDays(_random.Next(1, 7));
                }

                context.VerificationRequests.Add(request);
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedFriendshipsAsync(ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.Friendships.AnyAsync()) return;

            // Create a social network graph
            for (int i = 0; i < users.Count; i++)
            {
                // Each user connects with 3-10 other users
                int numConnections = _random.Next(3, 11);
                var potentialFriends = users.Where((u, index) => index != i).OrderBy(x => _random.Next()).Take(numConnections);

                foreach (var friend in potentialFriends)
                {
                    // Check if friendship already exists
                    var exists = await context.Friendships.AnyAsync(f =>
                        (f.RequesterId == users[i].Id && f.AddresseeId == friend.Id) ||
                        (f.RequesterId == friend.Id && f.AddresseeId == users[i].Id));

                    if (!exists)
                    {
                        var status = _random.Next(10) switch
                        {
                            0 => FriendshipStatus.Blocked,
                            1 => FriendshipStatus.Pending,
                            _ => FriendshipStatus.Accepted
                        };

                        context.Friendships.Add(new Friendship
                        {
                            RequesterId = users[i].Id,
                            AddresseeId = friend.Id,
                            Status = status,
                            CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 180))
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task<List<Community>> SeedCommunitiesAsync(ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.Communities.AnyAsync()) return await context.Communities.ToListAsync();

            var communities = new List<Community>();
            var communityDefinitions = new[]
            {
                ("Informatica Economica", "ineconomica", "Discutii despre programare, baze de date si sisteme informatice in economie"),
                ("Cibernetica", "cibernetica", "Modelare economica, simulari si sisteme dinamice"),
                ("Statistica si Econometrie", "statistica", "Analiza datelor, statistica aplicata si modele econometrice"),
                ("Contabilitate si Informatica de Gestiune", "contabilitate", "Contabilitate financiara, audit si sisteme informatice contabile"),
                ("Marketing", "marketing", "Cercetari de marketing, comportamentul consumatorului si branding"),
                ("Finante si Asigurari", "finante", "Piete financiare, investitii si managementul riscului"),
                ("Management", "management", "Management organizational, resurse umane si antreprenoriat"),
                ("Economie si Afaceri Internationale", "economie", "Macroeconomic, comert international si politici economice"),
                ("Informatica", "informatica", "Programare, algoritmi, structuri de date si tehnologii web"),
                ("Inteligenta Artificiala", "ai", "Machine learning, deep learning si aplicatii AI in business"),
                ("Big Data si Analytics", "bigdata", "Procesarea datelor masive, business intelligence si analytics"),
                ("Cyber Security", "cybersecurity", "Securitate cibernetica, protectia datelor si ethical hacking")
            };

            var admins = users.Where(u => u.Role == Role.Admin || u.Role == Role.Moderator).ToList();

            foreach (var (name, slug, description) in communityDefinitions)
            {
                var creator = admins[_random.Next(admins.Count)];
                var community = new Community
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Slug = slug,
                    Description = description,
                    ImageUrl = $"https://picsum.photos/id/{_random.Next(1, 200)}/200/200",
                    CreatorId = creator.Id,
                    CreatedAt = DateTime.UtcNow.AddMonths(-_random.Next(1, 18))
                };

                communities.Add(community);
                context.Communities.Add(community);
            }

            await context.SaveChangesAsync();

            // Add memberships
            foreach (var community in communities)
            {
                // Add 20-40 members per community
                var memberCount = _random.Next(20, 41);
                var selectedUsers = users.OrderBy(x => _random.Next()).Take(memberCount).ToList();

                foreach (var user in selectedUsers)
                {
                    var role = user.Role == Role.Admin ? CommunityRole.Admin :
                              user.Role == Role.Moderator ? CommunityRole.Moderator :
                              CommunityRole.Member;

                    context.CommunityMemberships.Add(new CommunityMembership
                    {
                        CommunityId = community.Id,
                        UserId = user.Id,
                        Role = role,
                        IsMuted = _random.NextDouble() < 0.1, // 10% muted
                        JoinedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 365))
                    });
                }
            }

            await context.SaveChangesAsync();
            return communities;
        }

        private static async Task<List<AskNLearn.Domain.Entities.StudyGroup.StudyGroup>> SeedStudyGroupsAsync(ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.StudyGroups.AnyAsync()) return await context.StudyGroups.ToListAsync();

            var groups = new List<AskNLearn.Domain.Entities.StudyGroup.StudyGroup>();
            var groupDefinitions = new[]
            {
                ("Pregatire Licenta Informatica Economica", "Grup de studiu pentru pregatirea examenului de licenta la Informatica Economica", "Programming"),
                ("Advanced C# and .NET", "Deep dive into C# advanced features and .NET Core", "Programming"),
                ("Machine Learning Study Group", "Weekly meetings to discuss ML papers and implement algorithms", "AI"),
                ("Data Structures & Algorithms", "LeetCode prep and algorithm analysis", "Computer Science"),
                ("Econometrie Aplicata", "Grup pentru proiectele practice la Econometrie", "Economics"),
                ("Python for Data Science", "Learning pandas, numpy, scikit-learn together", "Data Science"),
                ("Baze de Date - Proiect", "Colaborare pentru proiectul la Baze de Date", "Databases"),
                ("Financial Modeling", "Excel and Python for financial analysis", "Finance"),
                ("Research Paper Reading Group", "Weekly discussions of recent CS papers", "Research"),
                ("Web Development Bootcamp", "Building full-stack applications from scratch", "Web Dev"),
                ("Contabilitate - Cazuri Practice", "Studii de caz si aplicatii practice", "Accounting"),
                ("Marketing Analytics", "Data-driven marketing strategies", "Marketing"),
                ("Blockchain and Cryptocurrencies", "Understanding blockchain technology and crypto markets", "Tech"),
                ("Competitive Programming", "Preparing for programming contests", "Algorithms"),
                ("Soft Skills for Tech Professionals", "Communication, leadership, and teamwork", "Professional Development")
            };

            var owners = users.Where(u => u.Role == Role.Moderator || u.Role == Role.Admin).ToList();

            foreach (var (name, description, subjectArea) in groupDefinitions)
            {
                var owner = owners[_random.Next(owners.Count)];
                var group = new AskNLearn.Domain.Entities.StudyGroup.StudyGroup
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description,
                    OwnerId = owner.Id,
                    IconUrl = $"https://picsum.photos/id/{_random.Next(1, 200)}/100/100",
                    IsPublic = _random.NextDouble() > 0.3, // 70% public
                    InviteCode = _random.Next(100000, 999999).ToString(),
                    SubjectArea = subjectArea,
                    CreatedAt = DateTime.UtcNow.AddMonths(-_random.Next(1, 12))
                };

                groups.Add(group);
                context.StudyGroups.Add(group);

                // Add owner as member
                context.GroupMemberships.Add(new GroupMembership
                {
                    GroupId = group.Id,
                    UserId = owner.Id,
                    JoinedAt = group.CreatedAt
                });
            }

            await context.SaveChangesAsync();

            // Add members to groups
            foreach (var group in groups)
            {
                var memberCount = _random.Next(5, 20);
                var members = users.Where(u => u.Id != group.OwnerId)
                                  .OrderBy(x => _random.Next())
                                  .Take(memberCount)
                                  .ToList();

                foreach (var member in members)
                {
                    context.GroupMemberships.Add(new GroupMembership
                    {
                        GroupId = group.Id,
                        UserId = member.Id,
                        JoinedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 180))
                    });
                }
            }

            await context.SaveChangesAsync();
            return groups;
        }

        private static async Task SeedGroupAddonsAsync(ApplicationDbContext context,
            List<AskNLearn.Domain.Entities.StudyGroup.StudyGroup> groups,
            List<ApplicationUser> users)
        {
            if (await context.GroupRoles.AnyAsync()) return;

            foreach (var group in groups)
            {
                // Create roles
                var adminRole = new GroupRole
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    Name = "Admin",
                    Color = "#FF4444",
                    Permissions = "ALL",
                    Priority = 100
                };

                var modRole = new GroupRole
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    Name = "Moderator",
                    Color = "#FFAA44",
                    Permissions = "MANAGE_MESSAGES,KICK,BAN",
                    Priority = 50
                };

                var helperRole = new GroupRole
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    Name = "Helper",
                    Color = "#44FF44",
                    Permissions = "MUTE,MOVE",
                    Priority = 25
                };

                context.GroupRoles.AddRange(adminRole, modRole, helperRole);

                // Create categories
                var textCategory = new ChannelCategory
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    Name = "TEXT CHANNELS",
                    Position = 0
                };

                var voiceCategory = new ChannelCategory
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    Name = "VOICE CHANNELS",
                    Position = 1
                };

                context.ChannelCategories.AddRange(textCategory, voiceCategory);
            }

            await context.SaveChangesAsync();
        }

        private static async Task<List<Channel>> SeedChannelsAsync(ApplicationDbContext context,
            List<AskNLearn.Domain.Entities.StudyGroup.StudyGroup> groups)
        {
            if (await context.Channels.AnyAsync()) return await context.Channels.ToListAsync();

            var channels = new List<Channel>();

            foreach (var group in groups)
            {
                // Text channels
                var generalChannel = new Channel
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    Name = "general",
                    Type = ChannelType.Text,
                    Topic = "General discussion for " + group.Name,
                    Position = 0
                };

                var resourcesChannel = new Channel
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    Name = "resources",
                    Type = ChannelType.Text,
                    Topic = "Share useful resources and links",
                    Position = 1
                };

                var announcementsChannel = new Channel
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    Name = "announcements",
                    Type = ChannelType.Text,
                    Topic = "Important announcements",
                    IsPrivate = false,
                    Position = 2
                };

                var qaChannel = new Channel
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    Name = "q-and-a",
                    Type = ChannelType.Text,
                    Topic = "Questions and answers",
                    Position = 3
                };

                channels.AddRange(new[] { generalChannel, resourcesChannel, announcementsChannel, qaChannel });
                context.Channels.AddRange(generalChannel, resourcesChannel, announcementsChannel, qaChannel);

                // Voice channels
                var voiceGeneral = new Channel
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    Name = "General Voice",
                    Type = ChannelType.Voice,
                    Position = 0
                };

                var voiceStudy = new Channel
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    Name = "Study Room",
                    Type = ChannelType.Voice,
                    Position = 1
                };

                channels.AddRange(new[] { voiceGeneral, voiceStudy });
                context.Channels.AddRange(voiceGeneral, voiceStudy);
            }

            await context.SaveChangesAsync();
            return channels;
        }

        private static async Task<List<Post>> SeedPostsAsync(ApplicationDbContext context,
            List<Community> communities,
            List<Channel> channels,
            List<ApplicationUser> users)
        {
            if (await context.Posts.AnyAsync()) return await context.Posts.ToListAsync();

            var posts = new List<Post>();
            var tags = await context.Tags.ToListAsync();

            // Post templates for variety
            var postTitles = new[]
            {
                "Intrebare: {0}",
                "Tutorial: {0}",
                "Cum se implementeaza {0}?",
                "Resurse utile pentru {0}",
                "ProblemÄƒ la tema {0}",
                "Explicatie concept {0}",
                "Sfaturi pentru {0}",
                "Review carte: {0}",
                "Proiect {0} - feedback?",
                "Anunt important despre {0}"
            };

            var postContents = new[]
            {
                "Am nevoie de ajutor cu {0}. Poate cineva sÄƒ explice?",
                "Am gÄƒsit acest tutorial excelent pentru {0}: [link]",
                "Cum abordaÈ›i de obicei {0} Ã®n practicÄƒ?",
                "IatÄƒ cÃ¢teva resurse utile pentru a Ã®nvÄƒÈ›a {0}: {1}",
                "MÄƒ confrunt cu o problemÄƒ la {0}. Eroarea este: NullReferenceException",
                "Am creat un proiect open-source pentru {0}. PÄƒreri?",
                "Care sunt cele mai bune practici pentru {0}?",
                "Recomand aceastÄƒ carte pentru {0}: 'Clean Code'",
                "Am implementat {0} folosind arhitecturÄƒ clean. Codul: [link]",
                "AtenÈ›ie! Termenul limitÄƒ pentru {0} este sÄƒptÄƒmÃ¢na viitoare."
            };

            var subjects = new[]
            {
                "programare orientatÄƒ pe obiecte", "baze de date relaÈ›ionale", "algoritmi de sortare",
                "machine learning", "dezvoltare web", "API design", "testare unitarÄƒ",
                "design patterns", "SQL queries", "NoSQL databases", "cloud computing",
                "securitate ciberneticÄƒ", "structuri de date", "analiza algoritmilor",
                "econometrie", "serii temporale", "regresie liniarÄƒ", "deep learning"
            };

            // Create posts in communities
            foreach (var community in communities)
            {
                int postCount = _random.Next(10, 25);

                for (int i = 0; i < postCount; i++)
                {
                    var author = users[_random.Next(users.Count)];
                    var subject = subjects[_random.Next(subjects.Length)];
                    var title = string.Format(postTitles[_random.Next(postTitles.Length)], subject);
                    var content = string.Format(postContents[_random.Next(postContents.Length)],
                        subject,
                        $"https://example.com/resource/{_random.Next(1000, 9999)}");

                    var post = new Post
                    {
                        Id = Guid.NewGuid(),
                        CommunityId = community.Id,
                        AuthorId = author.Id,
                        Title = title.Length > 255 ? title.Substring(0, 252) + "..." : title,
                        Content = content,
                        IsSolved = _random.NextDouble() > 0.4,
                        IsLocked = _random.NextDouble() > 0.95, // 5% locked
                        ViewCount = _random.Next(50, 5000),
                        CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 180))
                    };

                    posts.Add(post);
                    context.Posts.Add(post);

                    // Add tags to post
                    int tagCount = _random.Next(1, 4);
                    var selectedTags = tags.OrderBy(x => _random.Next()).Take(tagCount).ToList();
                    foreach (var tag in selectedTags)
                    {
                        context.PostTags.Add(new PostTag { PostId = post.Id, TagId = tag.Id, Post = post, Tag = tag });
                    }
                }
            }

            // Create posts in study group channels
            foreach (var channel in channels.Where(c => c.Type == ChannelType.Text))
            {
                int postCount = _random.Next(5, 15);

                for (int i = 0; i < postCount; i++)
                {
                    var author = users[_random.Next(users.Count)];
                    var subject = subjects[_random.Next(subjects.Length)];

                    var post = new Post
                    {
                        Id = Guid.NewGuid(),
                        ChannelId = channel.Id,
                        AuthorId = author.Id,
                        Title = $"Discussion in {channel.Name}: {subject}",
                        Content = $"Let's talk about {subject} in the context of our study group.",
                        IsPinned = _random.NextDouble() > 0.9,
                        CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 90))
                    };

                    posts.Add(post);
                    context.Posts.Add(post);
                }
            }

            await context.SaveChangesAsync();
            return posts;
        }

        private static async Task SeedMessagesAsync(ApplicationDbContext context,
            List<Post> posts,
            List<Channel> channels,
            List<ApplicationUser> users)
        {
            if (await context.Messages.AnyAsync()) return;

            var messages = new List<Message>();

            // Comments on posts (2-8 comments per post)
            foreach (var post in posts)
            {
                int commentCount = _random.Next(2, 9);
                var commenters = users.OrderBy(x => _random.Next()).Take(commentCount).ToList();

                foreach (var commenter in commenters)
                {
                    var message = new Message
                    {
                        Id = Guid.NewGuid(),
                        PostId = post.Id,
                        AuthorId = commenter.Id,
                        Content = GenerateCommentContent(post.Title),
                        CreatedAt = post.CreatedAt.AddHours(_random.Next(1, 720)) // Up to 30 days later
                    };

                    messages.Add(message);
                    context.Messages.Add(message);

                    // Add replies to some comments
                    if (_random.NextDouble() > 0.7)
                    {
                        var replier = users[_random.Next(users.Count)];
                        var reply = new Message
                        {
                            Id = Guid.NewGuid(),
                            PostId = post.Id,
                            AuthorId = replier.Id,
                            Content = "Sunt de acord! " + GenerateCommentContent(post.Title),
                            ReplyToMessageId = message.Id,
                            CreatedAt = message.CreatedAt.AddHours(_random.Next(1, 48))
                        };

                        messages.Add(reply);
                        context.Messages.Add(reply);
                    }
                }
            }

            // Channel messages (10-50 messages per channel)
            foreach (var channel in channels.Where(c => c.Type == ChannelType.Text))
            {
                int messageCount = _random.Next(10, 51);
                var channelUsers = users.OrderBy(x => _random.Next()).Take(_random.Next(5, 15)).ToList();

                for (int i = 0; i < messageCount; i++)
                {
                    var author = channelUsers[_random.Next(channelUsers.Count)];
                    var message = new Message
                    {
                        Id = Guid.NewGuid(),
                        ChannelId = channel.Id,
                        AuthorId = author.Id,
                        Content = GenerateChannelMessage(channel.Name),
                        CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 30))
                            .AddMinutes(-_random.Next(0, 1440))
                    };

                    messages.Add(message);
                    context.Messages.Add(message);

                    // Add reactions to some messages
                    if (_random.NextDouble() > 0.5)
                    {
                        int reactionCount = _random.Next(1, 6);
                        var reactors = channelUsers.OrderBy(x => _random.Next()).Take(reactionCount).ToList();

                        foreach (var reactor in reactors)
                        {
                            context.MessageReactions.Add(new MessageReaction
                            {
                                MessageId = message.Id,
                                UserId = reactor.Id,
                                EmojiCode = GetRandomEmoji()
                            });
                        }
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private static string GenerateCommentContent(string postTitle)
        {
            var comments = new[]
            {
                "MulÈ›umesc pentru Ã®ntrebare! È˜i eu eram confuz.",
                "Conform cursului, conceptul se referÄƒ la...",
                "Am gÄƒsit o resursÄƒ utilÄƒ: [link]",
                "Exact ce cÄƒutam!",
                "Nu sunt sigur, dar cred cÄƒ...",
                "AÈ™ putea sÄƒ explic pe scurt...",
                "Recomand sÄƒ consulÈ›i capitolul 5 din manual.",
                "Am implementat asta recent. IatÄƒ codul:",
                "ExcelentÄƒ Ã®ntrebare! MÄƒ gÃ¢ndeam È™i eu la asta.",
                "Profesorul a menÈ›ionat Ã®n seminar cÄƒ..."
            };

            return comments[_random.Next(comments.Length)];
        }

        private static string GenerateChannelMessage(string channelName)
        {
            var messages = new[]
            {
                "Salutare tuturor! Cum merge treaba?",
                "AÈ›i vÄƒzut noul update?",
                "Am o Ã®ntrebare legatÄƒ de tema asta...",
                "MulÈ›umesc pentru ajutor!",
                "Cine mai lucreazÄƒ la proiect disearÄƒ?",
                "Link-ul acesta poate fi util: https://docs.example.com",
                "SÄƒ verificÄƒm Ã®mpreunÄƒ mÃ¢ine?",
                "Am Ã®ncÄƒrcat resursele Ã®n canal.",
                "BunÄƒ dimineaÈ›a!",
                "Weekend productiv!",
                "Apropo de discuÈ›ia de ieri...",
                "Nu Ã®nÈ›eleg aceastÄƒ parte din cod.",
                "Excelent! MulÈ›umesc pentru explicaÈ›ie.",
                "CÃ¢nd este deadline-ul?",
                "Am creat un repository pe GitHub."
            };

            return messages[_random.Next(messages.Length)];
        }

        private static string GetRandomEmoji()
        {
            var emojis = new[] { "ðŸ‘", "â¤ï¸", "ðŸ˜‚", "ðŸ˜®", "ðŸ˜¢", "ðŸ‘Ž", "ðŸ”¥", "ðŸŽ‰", "ðŸ’¯", "ðŸ¤”", "ðŸ‘", "ðŸ™" };
            return emojis[_random.Next(emojis.Length)];
        }

        private static async Task SeedEngagementAsync(ApplicationDbContext context,
            List<Post> posts,
            List<Message> messages,
            List<ApplicationUser> users)
        {
            if (await context.PostVotes.AnyAsync()) return;

            // Post votes and views
            foreach (var post in posts)
            {
                int voterCount = _random.Next(5, 31);
                var voters = users.OrderBy(x => _random.Next()).Take(voterCount).ToList();

                foreach (var voter in voters)
                {
                    // Upvote or downvote
                    var voteValue = _random.NextDouble() > 0.2 ? (short)1 : (short)-1;

                    context.PostVotes.Add(new PostVote
                    {
                        PostId = post.Id,
                        UserId = voter.Id,
                        VoteValue = voteValue
                    });

                    // Track unique views
                    if (_random.NextDouble() > 0.3)
                    {
                        context.PostViews.Add(new PostView
                        {
                            PostId = post.Id,
                            UserId = voter.Id,
                            ViewedAt = post.CreatedAt.AddHours(_random.Next(1, 720))
                        });
                    }
                }
            }

            // Message reactions
            foreach (var message in messages.Take(200))
            {
                int reactorCount = _random.Next(1, 8);
                var reactors = users.OrderBy(x => _random.Next()).Take(reactorCount).ToList();

                foreach (var reactor in reactors)
                {
                    context.MessageReactions.Add(new MessageReaction
                    {
                        MessageId = message.Id,
                        UserId = reactor.Id,
                        EmojiCode = GetRandomEmoji()
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedDirectMessagingAsync(ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.DirectConversations.AnyAsync()) return;

            // Create conversation pairs
            var conversationPairs = new HashSet<(string, string)>();
            int conversationCount = _random.Next(20, 41);

            for (int i = 0; i < conversationCount; i++)
            {
                var user1 = users[_random.Next(users.Count)];
                var user2 = users[_random.Next(users.Count)];

                if (user1.Id == user2.Id) continue;

                var pair1 = (user1.Id, user2.Id);
                var pair2 = (user2.Id, user1.Id);

                if (conversationPairs.Contains(pair1) || conversationPairs.Contains(pair2))
                    continue;

                conversationPairs.Add(pair1);

                var conversation = new DirectConversation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 120))
                };

                context.DirectConversations.Add(conversation);

                context.DirectConversationParticipants.Add(new DirectConversationParticipant
                {
                    ConversationId = conversation.Id,
                    UserId = user1.Id
                });

                context.DirectConversationParticipants.Add(new DirectConversationParticipant
                {
                    ConversationId = conversation.Id,
                    UserId = user2.Id
                });

                // Add messages to conversation
                int messageCount = _random.Next(2, 21);
                var participants = new[] { user1, user2 };

                for (int j = 0; j < messageCount; j++)
                {
                    var author = participants[_random.Next(2)];

                    context.Messages.Add(new Message
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = conversation.Id,
                        AuthorId = author.Id,
                        Content = GenerateDirectMessage(),
                        CreatedAt = conversation.CreatedAt.AddHours(_random.Next(1, 720))
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        private static string GenerateDirectMessage()
        {
            var messages = new[]
            {
                "Salut! Ai vÄƒzut postarea despre proiect?",
                "BunÄƒ! Putem discuta mai tÃ¢rziu?",
                "MulÈ›umesc pentru ajutor ieri!",
                "Ai primit materialele pentru seminar?",
                "CÃ¢nd ai timp pentru o discuÈ›ie?",
                "Super! MÄƒ bucur cÄƒ am putut ajuta.",
                "Ai rezolvat problema?",
                "Putem colabora la tema asta?",
                "Ce pÄƒrere ai despre curs?",
                "Ne vedem mÃ¢ine la laborator?"
            };

            return messages[_random.Next(messages.Length)];
        }

        private static async Task SeedAuditAndReportsAsync(ApplicationDbContext context,
            List<Post> posts,
            List<Message> messages,
            List<ApplicationUser> users)
        {
            if (await context.AuditLogs.AnyAsync()) return;

            // Create audit logs
            var actions = new[] { "LOGIN", "LOGOUT", "CREATE_POST", "EDIT_POST", "DELETE_POST",
                                  "CREATE_COMMENT", "EDIT_COMMENT", "DELETE_COMMENT",
                                  "JOIN_COMMUNITY", "LEAVE_COMMUNITY", "UPDATE_PROFILE",
                                  "UPLOAD_FILE", "VERIFICATION_REQUEST", "FRIEND_REQUEST" };

            foreach (var user in users.Take(30))
            {
                int logCount = _random.Next(5, 21);

                for (int i = 0; i < logCount; i++)
                {
                    context.AuditLogs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        ActorId = user.Id,
                        ActionType = actions[_random.Next(actions.Length)],
                        TargetEntity = _random.Next(3) switch
                        {
                            0 => "Post",
                            1 => "Message",
                            _ => "User"
                        },
                        TargetId = Guid.NewGuid(),
                        Changes = JsonSerializer.Serialize(new { field = "content", oldValue = "old", newValue = "new" }),
                        CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 90))
                    });
                }
            }

            // Create reports
            if (!await context.Reports.AnyAsync())
            {
                for (int i = 0; i < 15; i++)
                {
                    var reporter = users[_random.Next(users.Count)];
                    var reportedPost = posts[_random.Next(posts.Count)];
                    var reason = _random.Next(4) switch
                    {
                        0 => ReportReason.Spam,
                        1 => ReportReason.Harassment,
                        2 => ReportReason.Inappropriate,
                        _ => ReportReason.Other
                    };

                    context.Reports.Add(new Report
                    {
                        Id = Guid.NewGuid(),
                        ReporterId = reporter.Id,
                        ReportedPostId = reportedPost.Id,
                        Reason = reason,
                        Description = "This content violates community guidelines.",
                        Status = _random.Next(3) switch
                        {
                            0 => ReportStatus.Pending,
                            1 => ReportStatus.Resolved,
                            _ => ReportStatus.Dismissed
                        },
                        CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 30))
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedNotificationsAsync(ApplicationDbContext context, List<ApplicationUser> users)
        {
            if (await context.Notifications.AnyAsync()) return;

            var notificationTypes = new[]
            {
                ("New reply to your post", "Someone replied to your question"),
                ("Friend request", "You have a new friend request"),
                ("Verification approved", "Your account has been verified"),
                ("New message", "You received a new direct message"),
                ("Community update", "New post in your community"),
                ("Mention", "Someone mentioned you in a post"),
                ("Achievement unlocked", "You earned a new badge"),
                ("Event reminder", "Study group meeting in 1 hour"),
                ("Welcome!", "Thanks for joining AskNLearn"),
                ("Rank up!", "You've reached a new rank")
            };

            foreach (var user in users)
            {
                int notificationCount = _random.Next(3, 16);

                for (int i = 0; i < notificationCount; i++)
                {
                    var (title, message) = notificationTypes[_random.Next(notificationTypes.Length)];

                    context.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Title = title,
                        Message = message,
                        IsRead = _random.NextDouble() > 0.4, // 60% read
                        ReferenceId = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 30))
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedLearningResourcesAsync(ApplicationDbContext context,
            List<ApplicationUser> users,
            List<AskNLearn.Domain.Entities.StudyGroup.StudyGroup> groups)
        {
            if (await context.LearningResources.AnyAsync()) return;

            var resourceTypes = new[] { "PDF", "Video", "Link", "Code", "Presentation" };
            var topics = new[]
            {
                "Introduction to Programming",
                "Object-Oriented Design Patterns",
                "Database Normalization",
                "RESTful API Design",
                "Machine Learning Basics",
                "Time Series Analysis",
                "Financial Accounting Principles",
                "Marketing Strategy Framework",
                "Statistical Hypothesis Testing",
                "Cloud Architecture Patterns"
            };

            for (int i = 0; i < 50; i++)
            {
                var uploader = users[_random.Next(users.Count)];
                var group = groups[_random.Next(groups.Count)];
                var topic = topics[_random.Next(topics.Length)];

                context.LearningResources.Add(new LearningResource
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    UploaderId = uploader.Id,
                    Title = $"{topic} - Resource {i + 1}",
                    Description = $"Comprehensive resource about {topic} including examples and exercises.",
                    ResourceType = resourceTypes[_random.Next(resourceTypes.Length)],
                    Url = $"https://resources.asknlearn.com/{topic.ToLower().Replace(' ', '-')}-{i + 1}",
                    DownloadCount = _random.Next(10, 500),
                    CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 120))
                });
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedEventsAsync(ApplicationDbContext context,
            List<Community> communities,
            List<ApplicationUser> users)
        {
            if (await context.Events.AnyAsync()) return;

            var eventTypes = new[]
            {
                ("Study Session", "Virtual study session for exam preparation"),
                ("Guest Lecture", "Special lecture by industry expert"),
                ("Workshop", "Hands-on workshop on practical skills"),
                ("Q&A Session", "Open Q&A with professors"),
                ("Project Presentation", "Students present their projects"),
                ("Networking Event", "Connect with peers and professionals"),
                ("Hackathon", "24-hour coding competition"),
                ("Career Fair", "Meet with potential employers")
            };

            for (int i = 0; i < 25; i++)
            {
                var community = communities[_random.Next(communities.Count)];
                var organizer = users[_random.Next(users.Count)];
                var (eventType, description) = eventTypes[_random.Next(eventTypes.Length)];

                var startDate = DateTime.UtcNow.AddDays(_random.Next(-30, 60));

                context.Events.Add(new Event
                {
                    Id = Guid.NewGuid(),
                    CommunityId = community.Id,
                    OrganizerId = organizer.Id,
                    Title = $"{eventType}: {community.Name}",
                    Description = description,
                    Location = _random.Next(2) == 0 ? "Online (Zoom)" : "Room 101, Faculty Building",
                    StartTime = startDate,
                    EndTime = startDate.AddHours(_random.Next(1, 4)),
                    MaxAttendees = _random.Next(20, 101),
                    CurrentAttendees = _random.Next(5, 51),
                    CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 60))
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
