using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Application.Features.Posts.Queries.GetPostsByCommunity;
using AskNLearn.Domain.Entities.Core;
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
                .Where(p => p.Id == request.Id && p.ModerationStatus != ModerationStatus.Flagged)
                .Include(p => p.Author)
                .Include(p => p.Attachments)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
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
                    CreatedAt = p.CreatedAt,
                    ModerationStatus = p.ModerationStatus,
                    ModerationReason = p.ModerationReason,
                    Comments = p.Comments
                        .Where(c => c.ModerationStatus != ModerationStatus.Flagged)
                        .OrderBy(c => c.CreatedAt)
                        .Select(c => new CommentDto
                    {
                        Id = c.Id,
                        AuthorName = c.Author != null ? c.Author.FullName : "Unknown",
                        Content = c.Content ?? "",
                        CreatedAt = c.CreatedAt,
                        ModerationStatus = c.ModerationStatus,
                        ModerationReason = c.ModerationReason,
                        ReplyToMessageId = c.ReplyToMessageId,
                        Attachments = c.Attachments != null ? c.Attachments.Select(a => new AttachmentDto
                        {
                            Id = a.FileId,
                            Url = a.File != null ? a.File.FilePath : "",
                            FileType = a.File != null ? a.File.FileType : ""
                        }).ToList() : new List<AttachmentDto>()
                    }).ToList(),
                    Attachments = p.Attachments.Select(a => new AttachmentDto
                    {
                        Id = a.Id,
                        Url = a.Url,
                        FileType = a.FileType
                    }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
