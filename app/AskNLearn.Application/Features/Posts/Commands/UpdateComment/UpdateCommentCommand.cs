using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Posts.Commands.UpdateComment
{
    public class UpdateCommentCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string Content { get; set; } = null!;
    }

    public class UpdateCommentCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateCommentCommand, bool>
    {
        public async Task<bool> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserId)) return false;

            var comment = await context.Messages
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (comment == null) return false;
            
            // Authorization check
            if (comment.AuthorId != request.UserId) return false;

            comment.Content = request.Content;
            // Optionally update a "ModifiedAt" field if it exists
            
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
