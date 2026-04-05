using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Application.Common.Models;
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
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            var posts = await _context.Posts
                .Where(p => p.CommunityId == request.CommunityId && p.ModerationStatus != ModerationStatus.Flagged)
                .Include(p => p.Author)
                .Include(p => p.Attachments)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
                    AuthorConnectionStatus = string.IsNullOrEmpty(request.CurrentUserId) ? ConnectionStatus.None : 
                        _context.Friendships.Any(f => (f.RequesterId == request.CurrentUserId && f.AddresseeId == p.AuthorId && f.Status == FriendshipStatus.Accepted) || 
                                                     (f.RequesterId == p.AuthorId && f.AddresseeId == request.CurrentUserId && f.Status == FriendshipStatus.Accepted)) ? ConnectionStatus.Accepted :
                        _context.Friendships.Any(f => f.RequesterId == request.CurrentUserId && f.AddresseeId == p.AuthorId && f.Status == FriendshipStatus.Pending) ? ConnectionStatus.PendingSent :
                        _context.Friendships.Any(f => f.RequesterId == p.AuthorId && f.AddresseeId == request.CurrentUserId && f.Status == FriendshipStatus.Pending) ? ConnectionStatus.PendingReceived : ConnectionStatus.None,
                    // Comments are NOT loaded here — lazy loaded via GetPostCommentsQuery
                    Comments = new List<CommentDto>(),
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
