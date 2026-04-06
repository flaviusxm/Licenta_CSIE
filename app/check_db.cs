using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Infrastructure.Persistance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DBCheck
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("d:/Licenta_CSIE/app/AskNLearn.Web/appsettings.json")
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");
            Console.WriteLine($"Checking DB: {connectionString.Split(';')[0]}");

            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var userCount = await context.Users.CountAsync();
                var postCount = await context.Posts.CountAsync();
                var commCount = await context.Communities.CountAsync();
                var msgCount = await context.Messages.CountAsync();
                var verifCount = await context.VerificationRequests.CountAsync();

                Console.WriteLine($"Users: {userCount}");
                Console.WriteLine($"Posts: {postCount}");
                Console.WriteLine($"Communities: {commCount}");
                Console.WriteLine($"Messages: {msgCount}");
                Console.WriteLine($"Verification Requests: {verifCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
