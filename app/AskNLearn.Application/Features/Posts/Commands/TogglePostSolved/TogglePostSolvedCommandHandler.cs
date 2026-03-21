using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Posts.Commands.TogglePostSolved
{
    public class TogglePostSolvedCommandHandler : IRequestHandler<TogglePostSolvedCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public TogglePostSolvedCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(TogglePostSolvedCommand request, CancellationToken cancellationToken)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
            
            if (post == null) return false;

            // Authorization check
            if (post.AuthorId != request.UserId) return false;

            post.IsSolved = !post.IsSolved;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
