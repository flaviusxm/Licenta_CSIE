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
        private readonly IFileService _fileService;

        public CreateCommunityCommandHandler(IApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<Guid> Handle(CreateCommunityCommand request, CancellationToken cancellationToken)
        {
            var slug = request.Slug ?? request.Name.ToLower().Replace(" ", "-");
            
            string? imageUrl = null;
            if (request.Image != null)
            {
                using var stream = request.Image.OpenReadStream();
                imageUrl = await _fileService.UploadFileAsync(stream, request.Image.FileName, "communities");
            }

            var community = new Community
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = slug,
                Description = request.Description,
                ImageUrl = imageUrl,
                CreatorId = request.CreatorId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Communities.AddAsync(community, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return community.Id;
        }
    }
}
