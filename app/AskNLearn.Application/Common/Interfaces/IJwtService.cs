using AskNLearn.Domain.Entities.Core;
using System.Security.Claims;

namespace AskNLearn.Application.Common.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(ApplicationUser user, IEnumerable<Claim>? additionalClaims = null);
    }
}
