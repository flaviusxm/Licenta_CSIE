using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.StudyGroup;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.StudyGroups.Commands.CreateChannel
{
    public class CreateChannelCommandHandler : IRequestHandler<CreateChannelCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public CreateChannelCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateChannelCommand request, CancellationToken cancellationToken)
        {
            // Get the max position for the channel type within the group
            var maxPosition = await _context.Channels
                .Where(c => c.GroupId == request.GroupId && c.Type == request.Type)
                .Select(c => (int?)c.Position)
                .MaxAsync(cancellationToken) ?? -1;

            var channel = new Channel
            {
                Id = Guid.NewGuid(),
                GroupId = request.GroupId,
                Name = request.Name,
                Type = request.Type,
                Topic = request.Topic,
                Position = maxPosition + 1
            };

            await _context.Channels.AddAsync(channel, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return channel.Id;
        }
    }
}
