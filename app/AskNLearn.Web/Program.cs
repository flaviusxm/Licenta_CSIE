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

// SignalR cu WebSockets forțat și erori detaliate
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

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
    options.LoginPath = "/identity/auth/authenticate";
    options.AccessDeniedPath = "/identity/auth/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EmailConfirmed", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "EmailConfirmed" && c.Value == "true")));
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

    levelSwitch.MinimumLevel = LogEventLevel.Warning;

    if (args.Contains("drop-seed"))
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("  ȘTERGERE DATE DIN TOATE TABELELE...");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");

        try
        {
            bool isSqlServer = dbContext.Database.IsSqlServer();
            
            if (isSqlServer)
            {
                await dbContext.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");
            }

            var tables = new[]
            {
                "AuditLogs", "Reports", "Notifications", "MessageReactions", "MessageAttachments", "Messages",
                "DirectConversationParticipants", "DirectConversations", "PostTags", "PostVotes", "PostViews",
                "PostAttachments", "Posts", "Friendships", "CommunityMemberships",
                "Communities", "StoredFiles", "VerificationRequests", "UserRoles", "UserClaims", "UserLogins",
                "UserTokens", "RoleClaims", "Roles", "Users", "UserRanks", "Tags"
            };

            foreach (var table in tables)
            {
                try
                {
                    // Using a hardcoded string construction to avoid the analyzer warning on interpolated strings for table names
                    string sql = "DELETE FROM [" + table + "]";
                    int deleted = await dbContext.Database.ExecuteSqlRawAsync(sql);
                    if (deleted > 0)
                        Console.WriteLine($"  ✓ Șters: {table} ({deleted} rânduri)");
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("Invalid object name"))
                        Console.WriteLine($"  ⚠ {table}: {ex.Message.Substring(0, Math.Min(50, ex.Message.Length))}");
                }
            }

            if (isSqlServer)
            {
                await dbContext.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");
            }

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

// ─── DATABASE SCHEMA SYNC (Prevent Crashes) ───────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Forțăm crearea bazei de date dacă nu există, apoi migrări
        if (app.Environment.IsDevelopment())
        {
            await dbContext.Database.OpenConnectionAsync();
            await dbContext.Database.ExecuteSqlRawAsync(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'StoredFiles')
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[StoredFiles]') AND name = 'IsSafe')
                    BEGIN
                        ALTER TABLE [StoredFiles] ADD [IsSafe] BIT NOT NULL DEFAULT 1;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[StoredFiles]') AND name = 'SecurityNotes')
                    BEGIN
                        ALTER TABLE [StoredFiles] ADD [SecurityNotes] NVARCHAR(MAX) NULL;
                    END
                END
            ");
            await dbContext.Database.CloseConnectionAsync();
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Schema sync failed - this is normal if DB is not SQL Server or just initialized.");
    }
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
app.UseWebSockets();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Middleware pentru email neverificat – blochează accesul în afara paginilor de auth
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.User);
        if (user != null && !user.EmailConfirmed)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (!path.StartsWith("/identity/auth") && !path.StartsWith("/lib") && !path.StartsWith("/css") && !path.StartsWith("/js") && !path.StartsWith("/images") && !path.StartsWith("/uploads"))
            {
                context.Response.Redirect("/identity/auth/verify-email-notice");
                return;
            }
        }
    }
    await next();
});

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