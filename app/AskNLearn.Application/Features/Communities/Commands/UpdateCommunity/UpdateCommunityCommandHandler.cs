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
        private readonly IFileService _fileService;

        public UpdateCommunityCommandHandler(IApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<bool> Handle(UpdateCommunityCommand request, CancellationToken cancellationToken)
        {
            var community = await _context.Communities.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
            
            if (community == null) return false;

            if (request.Image != null)
            {
                using var stream = request.Image.OpenReadStream();
                var imageUrl = await _fileService.UploadFileAsync(stream, request.Image.FileName, "communities");
                community.ImageUrl = imageUrl;
            }

            community.Name = request.Name;
            community.Description = request.Description;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
