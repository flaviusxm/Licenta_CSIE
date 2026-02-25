using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Posts.Commands.UpdatePost
{
    public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public UpdatePostCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
            
            if (post == null) return false;

            post.Title = request.Title;
            post.Content = request.Content;
            post.IsSolved = request.IsSolved;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
