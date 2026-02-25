using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Communities.Commands.UpdateCommunity
{
    public class UpdateCommunityCommandHandler : IRequestHandler<UpdateCommunityCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public UpdateCommunityCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateCommunityCommand request, CancellationToken cancellationToken)
        {
            var community = await _context.Communities.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
            
            if (community == null) return false;

            community.Name = request.Name;
            community.Description = request.Description;
            community.IsPublic = request.IsPublic;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
