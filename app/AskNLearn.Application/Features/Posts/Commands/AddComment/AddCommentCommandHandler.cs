using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Messaging;
using AskNLearn.Domain.Entities.Core;
using MediatR;
using System;
using System.Net.Mail;
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

        public AddCommentCommandHandler(IApplicationDbContext context, IFileService fileService, ILogger<AddCommentCommandHandler> logger)
        {
            _context = context;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task<Guid> Handle(AddCommentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var comment = new Message
                {
                    Id = Guid.NewGuid(),
                    PostId = request.PostId,
                    ReplyToMessageId = request.ReplyToMessageId,
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

                    var storedFile = new StoredFile
                    {
                        Id = Guid.NewGuid(),
                        FileName = request.Attachment.FileName,
                        FilePath = fileUrl,
                        FileType = request.Attachment.ContentType,
                        UploadedAt = DateTime.UtcNow,
                        UploaderId = request.AuthorId
                    };

                    // Add storedFile to context explicitly to be safe
                    _context.StoredFiles.Add(storedFile);

                    comment.Attachments.Add(new MessageAttachment
                    {
                        MessageId = comment.Id,
                        FileId = storedFile.Id,
                        File = storedFile
                    });
                }

                await _context.Messages.AddAsync(comment, cancellationToken);
                _logger.LogInformation("Saving comment to database for post {PostId}", request.PostId);
                await _context.SaveChangesAsync(cancellationToken);

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
