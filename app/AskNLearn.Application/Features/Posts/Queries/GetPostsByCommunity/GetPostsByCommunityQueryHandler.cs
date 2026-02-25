using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Posts.Queries.GetPostsByCommunity
{
    public class GetPostsByCommunityQueryHandler : IRequestHandler<GetPostsByCommunityQuery, List<PostDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetPostsByCommunityQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PostDto>> Handle(GetPostsByCommunityQuery request, CancellationToken cancellationToken)
        {
            return await _context.Posts
                .Where(p => p.CommunityId == request.CommunityId)
                .Include(p => p.Author)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostDto
                {
                    Id = p.Id,
                    CommunityId = p.CommunityId,
                    AuthorId = p.AuthorId,
                    AuthorName = p.Author != null ? p.Author.FullName : "Unknown",
                    Title = p.Title,
                    Content = p.Content,
                    IsSolved = p.IsSolved,
                    IsLocked = p.IsLocked,
                    ViewCount = p.ViewCount,
                    CommentCount = p.Comments.Count,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }
    }
}
