using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Communities.Commands.DeleteCommunity
{
    public class DeleteCommunityCommandHandler : IRequestHandler<DeleteCommunityCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public DeleteCommunityCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteCommunityCommand request, CancellationToken cancellationToken)
        {
            var community = await _context.Communities.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
            
            if (community == null) return false;

            _context.Communities.Remove(community);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
