using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Core;
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
            var posts = await _context.Posts
                .Where(p => p.CommunityId == request.CommunityId && p.ModerationStatus != ModerationStatus.Flagged)
                .Include(p => p.Author)
                .Include(p => p.Attachments)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
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
                    VoteCount = _context.PostVotes.Where(v => v.PostId == p.Id).Select(v => (int)v.VoteValue).Sum(),
                    UserVote = !string.IsNullOrEmpty(request.CurrentUserId) 
                        ? _context.PostVotes.Where(v => v.PostId == p.Id && v.UserId == request.CurrentUserId).Select(v => (int)v.VoteValue).FirstOrDefault()
                        : 0,
                    CreatedAt = p.CreatedAt,
                    ModerationStatus = p.ModerationStatus,
                    ModerationReason = p.ModerationReason,
                    Comments = p.Comments
                        .Where(c => c.ModerationStatus != ModerationStatus.Flagged)
                        .OrderBy(c => c.CreatedAt)
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
                .ToListAsync(cancellationToken);

            return posts;
        }
    }
}
