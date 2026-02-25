using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.SocialFeed;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Communities.Commands.CreateCommunity
{
    public class CreateCommunityCommandHandler : IRequestHandler<CreateCommunityCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public CreateCommunityCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateCommunityCommand request, CancellationToken cancellationToken)
        {
            var slug = request.Slug ?? request.Name.ToLower().Replace(" ", "-");
            
            var community = new Community
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = slug,
                Description = request.Description,
                CreatorId = request.CreatorId,
                IsPublic = request.IsPublic,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Communities.AddAsync(community, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return community.Id;
        }
    }
}
