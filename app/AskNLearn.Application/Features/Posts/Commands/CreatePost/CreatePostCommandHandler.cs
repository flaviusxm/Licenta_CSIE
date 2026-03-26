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
        private readonly IFileService _fileService;
        private readonly IModerationQueue _moderationQueue;

        public CreatePostCommandHandler(IApplicationDbContext context, IFileService fileService, IModerationQueue moderationQueue)
        {
            _context = context;
            _fileService = fileService;
            _moderationQueue = moderationQueue;
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

            if (request.Attachments != null && request.Attachments.Count > 0)
            {
                foreach (var file in request.Attachments)
                {
                    using var stream = file.OpenReadStream();
                    var url = await _fileService.UploadFileAsync(stream, file.FileName, "posts");
                    
                    post.Attachments.Add(new PostAttachment
                    {
                        PostId = post.Id,
                        Url = url,
                        FileType = file.ContentType
                    });
                }
            }

            await _context.Posts.AddAsync(post, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Enqueue for AI Moderation
            _moderationQueue.Enqueue(new ModerationTask
            {
                Id = post.Id,
                Content = post.Content,
                Title = post.Title,
                Target = ModerationTarget.Post
            });

            return post.Id;
        }
    }
}
