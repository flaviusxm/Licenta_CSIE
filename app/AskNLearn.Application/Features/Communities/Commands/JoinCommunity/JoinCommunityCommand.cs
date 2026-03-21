using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.SocialFeed;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Communities.Commands.JoinCommunity
{
    public class JoinCommunityCommand : IRequest<bool>
    {
        public Guid CommunityId { get; set; }
        public string? UserId { get; set; }
    }

    public class JoinCommunityCommandHandler(IApplicationDbContext context) : IRequestHandler<JoinCommunityCommand, bool>
    {
        public async Task<bool> Handle(JoinCommunityCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserId)) return false;

            var alreadyMember = await context.CommunityMemberships
                .AnyAsync(m => m.CommunityId == request.CommunityId && m.UserId == request.UserId, cancellationToken);

            if (alreadyMember) return true;

            context.CommunityMemberships.Add(new CommunityMembership
            {
                CommunityId = request.CommunityId,
                UserId = request.UserId,
                JoinedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
