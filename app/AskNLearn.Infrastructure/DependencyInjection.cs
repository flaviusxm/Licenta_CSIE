using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Infrastructure.Persistance;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using AskNLearn.Infrastructure.Services;

namespace AskNLearn.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString, b => 
            {
                b.MigrationsAssembly("AskNLearn.Infrastructure");
                b.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            
            services.AddScoped<IGuardianClient, GuardianClient>();
            services.AddScoped<IFileService, LocalFileService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddSingleton<IPresenceTracker, PresenceTracker>();

            return services;
        }
    }
}
