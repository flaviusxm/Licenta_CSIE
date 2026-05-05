using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.SocialFeed;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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

    public class VotePostCommandHandler(IApplicationDbContext context, IReputationService reputationService) : IRequestHandler<VotePostCommand, VotePostResult>
    {
        public async Task<VotePostResult> Handle(VotePostCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserId)) return new VotePostResult { Success = false };

            var vote = await context.PostVotes
                .FirstOrDefaultAsync(v => v.PostId == request.PostId && v.UserId == request.UserId, cancellationToken);

            var post = await context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken);
            if (post == null) return new VotePostResult { Success = false };

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

                    // Add reputation for NEW upvote
                    if (request.Value == 1 && !string.IsNullOrEmpty(post.AuthorId))
                    {
                        await reputationService.AddPointsAsync(post.AuthorId, 2);
                    }
                }
                else
                {
                    // Toggle off if same value, otherwise update to new value
                    if (vote.VoteValue == request.Value)
                    {
                        context.PostVotes.Remove(vote);
                        
                        // Remove reputation if upvote is toggled off
                        if (request.Value == 1 && !string.IsNullOrEmpty(post.AuthorId))
                        {
                            await reputationService.RemovePointsAsync(post.AuthorId, 2);
                        }
                        
                        request.Value = 0; // Set to 0 for the result
                    }
                    else
                    {
                        var oldValue = vote.VoteValue;
                        vote.VoteValue = request.Value;

                        // Adjust reputation based on change
                        if (!string.IsNullOrEmpty(post.AuthorId))
                        {
                            if (oldValue == -1 && request.Value == 1) // Downvote to Upvote
                                await reputationService.AddPointsAsync(post.AuthorId, 2);
                            else if (oldValue == 1 && request.Value == -1) // Upvote to Downvote
                                await reputationService.RemovePointsAsync(post.AuthorId, 2);
                        }
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
