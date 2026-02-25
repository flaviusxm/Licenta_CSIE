using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.SocialFeed;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Community> Communities { get; }
        DbSet<Post> Posts { get; }
        DbSet<ApplicationUser> Users { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
