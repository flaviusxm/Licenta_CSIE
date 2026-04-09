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


builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, b => b.MigrationsAssembly("AskNLearn.Infrastructure"));
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/SignIn";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});

builder.Services.AddControllersWithViews();

var app = builder.Build();
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
        Console.WriteLine("Force dropping database (terminating active connections)...");
        try 
        {
            var dbName = dbContext.Database.GetDbConnection().Database;
            await dbContext.Database.ExecuteSqlRawAsync(
                $@"SELECT pg_terminate_backend(pg_stat_activity.pid) 
                   FROM pg_stat_activity 
                   WHERE pg_stat_activity.datname = '{dbName}' 
                   AND pid <> pg_backend_pid();");
        } catch { /* Ignore if it fails (e.g. not Postgres or no permissions) */ }

        await dbContext.Database.EnsureDeletedAsync();
    }
    
    Console.WriteLine("Applying migrations...");
    await dbContext.Database.MigrateAsync();
    
    Console.WriteLine("Seeding database (ENTERPRISE profile - this will take a while)...");
    await LoadTestDatabaseSeeder.SeedAsync(dbContext, userManager, LoadTestDatabaseSeeder.ScaleProfile.Enterprise);
    
    Console.WriteLine("Database initialization complete!");
    return;
}

if (args.Contains("migratedb"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    Console.WriteLine("Database migration complete!");
    return;
}
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
