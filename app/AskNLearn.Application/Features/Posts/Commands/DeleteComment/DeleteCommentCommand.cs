using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Posts.Commands.DeleteComment
{
    public class DeleteCommentCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
    }

    public class DeleteCommentCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteCommentCommand, bool>
    {
        public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserId)) return false;

            var comment = await context.Messages
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (comment == null) return false;
            
            // Authorization check
            if (comment.AuthorId != request.UserId) return false;

            context.Messages.Remove(comment);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
