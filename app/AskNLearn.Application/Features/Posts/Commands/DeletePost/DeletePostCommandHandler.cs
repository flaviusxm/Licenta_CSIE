using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Posts.Commands.DeletePost
{
    public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileService _fileService;

        public DeletePostCommandHandler(IApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<bool> Handle(DeletePostCommand request, CancellationToken cancellationToken)
        {
            var post = await _context.Posts
                .Include(p => p.Attachments)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Attachments)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
            
            if (post == null) return false;

            foreach (var attachment in post.Attachments)
            {
                if (!string.IsNullOrEmpty(attachment.Url))
                {
                    _fileService.DeleteFile(attachment.Url);
                }
            }

            foreach (var comment in post.Comments)
            {
                foreach (var attachment in comment.Attachments)
                {
                    if (!string.IsNullOrEmpty(attachment.Url))
                    {
                        _fileService.DeleteFile(attachment.Url);
                    }
                }
                _context.Comments.Remove(comment);
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
