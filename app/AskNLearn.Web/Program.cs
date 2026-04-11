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
        Console.WriteLine("Force dropping database...");
        try 
        {
            await dbContext.Database.EnsureDeletedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Note: Database drop skipped or failed (it might not exist yet): {ex.Message}");
        }
    }
    
    Console.WriteLine("Applying migrations...");
    await dbContext.Database.MigrateAsync();
    
    Console.WriteLine("Seeding database (ENTERPRISE profile - this will take a while)...");
await LoadTestDatabaseSeeder.SeedAsync(dbContext, userManager);
    
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
