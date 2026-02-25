using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.SocialFeed;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Posts.Commands.CreatePost
{
    public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public CreatePostCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
        {
            var post = new Post
            {
                Id = Guid.NewGuid(),
                CommunityId = request.CommunityId,
                AuthorId = request.AuthorId,
                Title = request.Title,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Posts.AddAsync(post, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return post.Id;
        }
    }
}
