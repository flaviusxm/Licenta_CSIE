using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Communities.Queries.GetCommunities
{
    public class GetCommunitiesQueryHandler : IRequestHandler<GetCommunitiesQuery, List<CommunityDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetCommunitiesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CommunityDto>> Handle(GetCommunitiesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Communities
                .Include(c => c.Posts)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(c => c.Name.Contains(request.SearchTerm) || (c.Description != null && c.Description.Contains(request.SearchTerm)));
            }

            return await query
                .Select(c => new CommunityDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    CreatorId = c.CreatorId,
                    IsPublic = c.IsPublic,
                    CreatedAt = c.CreatedAt,
                    PostCount = c.Posts.Count
                    // MemberCount can be added later when CommunityMembership is fully connected
                })
                .ToListAsync(cancellationToken);
        }
    }
}
