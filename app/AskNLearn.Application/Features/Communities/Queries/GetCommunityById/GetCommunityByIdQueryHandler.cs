using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Application.Features.Communities.Queries.GetCommunities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Communities.Queries.GetCommunityById
{
    public class GetCommunityByIdQueryHandler : IRequestHandler<GetCommunityByIdQuery, CommunityDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetCommunityByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CommunityDto?> Handle(GetCommunityByIdQuery request, CancellationToken cancellationToken)
        {
            var community = await _context.Communities
                .Include(c => c.Posts)
                .Where(c => c.Id == request.Id)
                .Select(c => new CommunityDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    CreatorId = c.CreatorId,
                    CreatedAt = c.CreatedAt,
                    PostCount = c.Posts.Count,
                    MemberCount = _context.CommunityMemberships.Count(m => m.CommunityId == c.Id)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (community != null && !string.IsNullOrEmpty(request.CurrentUserId))
            {
                community.IsMember = await _context.CommunityMemberships
                    .AnyAsync(m => m.CommunityId == community.Id && m.UserId == request.CurrentUserId, cancellationToken);
            }

            return community;
        }
    }
}
