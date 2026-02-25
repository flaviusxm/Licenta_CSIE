using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Application.Features.Posts.Queries.GetPostsByCommunity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Posts.Queries.GetPostById
{
    public class GetPostByIdQueryHandler : IRequestHandler<GetPostByIdQuery, PostDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetPostByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PostDto?> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.Posts
                .Where(p => p.Id == request.Id)
                .Include(p => p.Author)
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
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
