using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.SocialFeed;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Posts.Commands.VotePost
{
    public class VotePostResult
    {
        public bool Success { get; set; }
        public int VoteCount { get; set; }
        public int UserVote { get; set; }
    }

    public class VotePostCommand : IRequest<VotePostResult>
    {
        public Guid PostId { get; set; }
        public string? UserId { get; set; }
        public short Value { get; set; } // 1 for upvote, -1 for downvote, 0 to remove
    }

    public class VotePostCommandHandler(IApplicationDbContext context) : IRequestHandler<VotePostCommand, VotePostResult>
    {
        public async Task<VotePostResult> Handle(VotePostCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserId)) return new VotePostResult { Success = false };

            var vote = await context.PostVotes
                .FirstOrDefaultAsync(v => v.PostId == request.PostId && v.UserId == request.UserId, cancellationToken);

            if (request.Value == 0)
            {
                if (vote != null)
                {
                    context.PostVotes.Remove(vote);
                }
            }
            else
            {
                if (vote == null)
                {
                    context.PostVotes.Add(new PostVote
                    {
                        PostId = request.PostId,
                        UserId = request.UserId,
                        VoteValue = request.Value
                    });
                }
                else
                {
                    // Toggle off if same value, otherwise update to new value
                    if (vote.VoteValue == request.Value)
                    {
                        context.PostVotes.Remove(vote);
                        request.Value = 0; // Set to 0 for the result
                    }
                    else
                    {
                        vote.VoteValue = request.Value;
                    }
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            // Calculate updated counts efficiently
            var voteCount = await context.PostVotes
                .Where(v => v.PostId == request.PostId)
                .Select(v => (int)v.VoteValue)
                .SumAsync(cancellationToken);

            var userVote = request.Value; // Since we just saved it

            return new VotePostResult
            {
                Success = true,
                VoteCount = voteCount,
                UserVote = userVote
            };
        }
    }
}
