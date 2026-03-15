using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.StudyGroups.Commands.DeleteChannel
{
    public class DeleteChannelCommandHandler : IRequestHandler<DeleteChannelCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public DeleteChannelCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteChannelCommand request, CancellationToken cancellationToken)
        {
            var channel = await _context.Channels
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (channel == null)
            {
                return false;
            }

            _context.Channels.Remove(channel);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
