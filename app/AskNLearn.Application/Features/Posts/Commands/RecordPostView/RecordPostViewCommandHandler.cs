using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.SocialFeed;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Posts.Commands.RecordPostView
{
    public class RecordPostViewCommandHandler : IRequestHandler<RecordPostViewCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public RecordPostViewCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(RecordPostViewCommand request, CancellationToken cancellationToken)
        {
            var alreadyViewed = await _context.PostViews
                .AnyAsync(pv => pv.PostId == request.PostId && pv.UserId == request.UserId, cancellationToken);

            if (!alreadyViewed)
            {
                var post = await _context.Posts
                    .FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken);

                if (post != null)
                {
                    post.ViewCount++;
                    
                    var postView = new PostView
                    {
                        PostId = request.PostId,
                        UserId = request.UserId,
                        ViewedAt = DateTime.UtcNow
                    };

                    await _context.PostViews.AddAsync(postView, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    return true;
                }
            }

            return false;
        }
    }
}
