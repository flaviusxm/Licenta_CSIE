using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Core;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AskNLearn.Application.Features.Posts.Commands.AddComment
{
    public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly ILogger<AddCommentCommandHandler> _logger;
        private readonly IModerationQueue _moderationQueue;
        private readonly IReputationService _reputationService;

        public AddCommentCommandHandler(
            IApplicationDbContext context, 
            IFileService fileService, 
            ILogger<AddCommentCommandHandler> logger, 
            IModerationQueue moderationQueue,
            IReputationService reputationService)
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
            _moderationQueue = moderationQueue;
            _reputationService = reputationService;
        }

        public async Task<Guid> Handle(AddCommentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var comment = new AskNLearn.Domain.Entities.SocialFeed.Comment
                {
                    Id = Guid.NewGuid(),
                    PostId = request.PostId,
                    ReplyToCommentId = request.ReplyToMessageId,
                    AuthorId = request.AuthorId,
                    Content = request.Content,
                    CreatedAt = DateTime.UtcNow
                };

                if (request.Attachment != null)
                {
                    _logger.LogInformation("Uploading attachment for comment on post {PostId}. FileName: {FileName}", request.PostId, request.Attachment.FileName);
                    var fileUrl = await _fileService.UploadFileAsync(
                        request.Attachment.OpenReadStream(),
                        request.Attachment.FileName,
                        "comments"
                    );

                    comment.Attachments.Add(new AskNLearn.Domain.Entities.SocialFeed.CommentAttachment
                    {
                        Id = Guid.NewGuid(),
                        CommentId = comment.Id,
                        Url = fileUrl,
                        FileType = request.Attachment.ContentType
                    });
                }

                await _context.Comments.AddAsync(comment, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // Enqueue for AI Moderation
                _moderationQueue.Enqueue(new ModerationTask
                {
                    Id = comment.Id,
                    Content = comment.Content ?? string.Empty,
                    Target = ModerationTarget.Comment
                });

                // Add Reputation Points
                if (!string.IsNullOrEmpty(request.AuthorId))
                {
                    await _reputationService.AddPointsAsync(request.AuthorId, 5);
                }

                return comment.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment for Post {PostId}", request.PostId);
                throw;
            }
        }
    }
}
