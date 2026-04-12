using AskNLearn.Application;
using AskNLearn.Domain.Entities.Core;
using AskNLearn.Infrastructure;
using AskNLearn.Infrastructure.Persistance;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var levelSwitch = new Serilog.Core.LoggingLevelSwitch();
levelSwitch.MinimumLevel = LogEventLevel.Debug;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(levelSwitch)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddSignalR();

// AI & Moderation Services
builder.Services.AddHttpClient<IOllamaService, OllamaService>();
builder.Services.AddSingleton<IModerationQueue, ModerationQueue>();
builder.Services.AddHostedService<ModerationBackgroundService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    // Removing fixed default schemes allows Identity cookies to be the default for MVC
    // While still allowing JWT for specific API controllers when requested.
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!))
    };
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/SignIn";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ─── SEEDING LOGIC ─────────────────────────────────────────────────────────────
if (args.Contains("drop-seed") || args.Contains("seeddb"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // Suppress logs during seeding
    levelSwitch.MinimumLevel = LogEventLevel.Warning;

    if (args.Contains("drop-seed"))
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("  ȘTERGERE DATE DIN TOATE TABELELE...");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");

        try
        {
            // Dezactivează foreign keys
            await dbContext.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");

            // Șterge datele din tabele în ordinea corectă (copii înainte de părinți)
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
                    int deleted = await dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}]");
                    if (deleted > 0)
                        Console.WriteLine($"  ✓ Șters: {table} ({deleted} rânduri)");
                }
                catch (Exception ex)
                {
                    // Ignorăm erorile - tabelul poate să nu existe
                    if (!ex.Message.Contains("Invalid object name"))
                        Console.WriteLine($"  ⚠ {table}: {ex.Message.Substring(0, Math.Min(50, ex.Message.Length))}");
                }
            }

            // Reactivează foreign keys
            await dbContext.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");

            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
            Console.WriteLine("  TOATE DATELE AU FOST ȘTERSE!");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Eroare la ștergere: {ex.Message}");
        }
    }

    Console.WriteLine("\n═══════════════════════════════════════════════════════════════════");
    Console.WriteLine("  APLICARE MIGRĂRI...");
    Console.WriteLine("═══════════════════════════════════════════════════════════════════");

    try
    {
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("  ✓ Migrări aplicate cu succes!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Eroare la migrări: {ex.Message}");
        Console.WriteLine("  Încerc să creez baza de date...");
        await dbContext.Database.EnsureCreatedAsync();
        Console.WriteLine("  ✓ Bază de date creată!");
    }

    Console.WriteLine("\n═══════════════════════════════════════════════════════════════════");
    Console.WriteLine("  ENTERPRISE SEED - POPULARE BAZĂ DE DATE");
    Console.WriteLine("  Acest proces va dura 20-30 minute pentru 100k+ înregistrări");
    Console.WriteLine("═══════════════════════════════════════════════════════════════════\n");

    var sw = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        await LoadTestDatabaseSeeder.SeedAsync(dbContext, userManager, force: args.Contains("drop-seed"));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[FATAL] Eroare în timpul seeding-ului: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"  Inner: {ex.InnerException.Message}");
        throw;
    }

    sw.Stop();

    Console.WriteLine("\n═══════════════════════════════════════════════════════════════════");
    Console.WriteLine($"  SEEDING COMPLET în {sw.Elapsed.TotalMinutes:F1} minute!");
    Console.WriteLine("═══════════════════════════════════════════════════════════════════");

    return;
}

if (args.Contains("migratedb"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    Console.WriteLine("Aplicare migrări...");
    await db.Database.MigrateAsync();
    Console.WriteLine("✓ Migrări aplicate!");
    return;
}

// ─── NORMAL APP STARTUP ───────────────────────────────────────────────────────
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<AskNLearn.Web.Hubs.CommunicationHub>("/communicationHub");

app.MapStaticAssets();

app.MapControllerRoute(
    name: "admin",
    pattern: "adminRoute/{action=Index}/{id?}",
    defaults: new { controller = "Admin" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

try
{
    Log.Information("Starting app...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}