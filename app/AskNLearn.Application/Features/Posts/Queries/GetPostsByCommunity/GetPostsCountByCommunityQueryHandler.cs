using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Posts.Queries.GetPostsByCommunity
{
    public class GetPostsCountByCommunityQueryHandler : IRequestHandler<GetPostsCountByCommunityQuery, int>
    {
        private readonly IApplicationDbContext _context;

        public GetPostsCountByCommunityQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(GetPostsCountByCommunityQuery request, CancellationToken cancellationToken)
        {
            return await _context.Posts
                .Where(p => p.CommunityId == request.CommunityId && p.ModerationStatus != ModerationStatus.Flagged)
                .CountAsync(cancellationToken);
        }
    }
}
