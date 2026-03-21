using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Communities.Commands.LeaveCommunity
{
    public class LeaveCommunityCommand : IRequest<bool>
    {
        public Guid CommunityId { get; set; }
        public string? UserId { get; set; }
    }

    public class LeaveCommunityCommandHandler(IApplicationDbContext context) : IRequestHandler<LeaveCommunityCommand, bool>
    {
        public async Task<bool> Handle(LeaveCommunityCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserId)) return false;

            var membership = await context.CommunityMemberships
                .FirstOrDefaultAsync(m => m.CommunityId == request.CommunityId && m.UserId == request.UserId, cancellationToken);

            if (membership == null) return true;

            context.CommunityMemberships.Remove(membership);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
