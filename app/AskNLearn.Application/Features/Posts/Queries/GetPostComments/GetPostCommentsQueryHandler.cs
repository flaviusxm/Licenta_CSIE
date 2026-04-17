using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Application.Features.Posts.Queries.GetPostsByCommunity;
using AskNLearn.Domain.Entities.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Posts.Queries.GetPostComments
{
    public class GetPostCommentsQueryHandler : IRequestHandler<GetPostCommentsQuery, PostCommentsResult>
    {
        private readonly IApplicationDbContext _context;

        public GetPostCommentsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PostCommentsResult> Handle(GetPostCommentsQuery request, CancellationToken cancellationToken)
        {
            var post = await _context.Posts
                .Where(p => p.Id == request.PostId)
                .Select(p => new { p.Id, p.AuthorId, p.IsSolved })
                .FirstOrDefaultAsync(cancellationToken);

            if (post == null)
                return new PostCommentsResult { PostId = request.PostId, CommunityId = request.CommunityId };

            var comments = await _context.Posts
                .Where(p => p.Id == request.PostId)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Attachments)
                        .ThenInclude(a => a.File)
                .SelectMany(p => p.Comments
                    .Where(c => c.ModerationStatus != ModerationStatus.Flagged && c.ModerationStatus != ModerationStatus.Removed)
                    .Select(c => new CommentDto
                    {
                        Id = c.Id,
                        AuthorId = c.AuthorId,
                        AuthorName = c.Author != null ? c.Author.FullName : "Unknown",
                        Content = c.Content ?? "",
                        CreatedAt = c.CreatedAt,
                        ModerationStatus = c.ModerationStatus,
                        ModerationReason = c.ModerationReason,
                        ReplyToMessageId = c.ReplyToMessageId,
                        Attachments = c.Attachments != null
                            ? c.Attachments.Select(a => new AttachmentDto
                            {
                                Id = a.FileId,
                                Url = a.File != null ? a.File.FilePath : "",
                                FileType = a.File != null ? a.File.FileType : ""
                            }).ToList()
                            : new System.Collections.Generic.List<AttachmentDto>()
                    }))
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

            return new PostCommentsResult
            {
                PostId = request.PostId,
                CommunityId = request.CommunityId,
                AuthorId = post.AuthorId,
                IsSolved = post.IsSolved,
                Comments = comments
            };
        }
    }
}
