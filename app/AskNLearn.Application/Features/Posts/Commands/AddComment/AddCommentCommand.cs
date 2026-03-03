using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using AskNLearn.Application.Common.Attributes;

namespace AskNLearn.Application.Features.Posts.Commands.AddComment
{
    public class AddCommentCommand : IRequest<Guid>
    {
        public Guid PostId { get; set; }
        public Guid? CommunityId { get; set; }
        public Guid? ReplyToMessageId { get; set; }
        public string? AuthorId { get; set; }

        public string? Content { get; set; }
        
        [AllowedExtensions(new string[] { ".pdf", ".docx", ".jpeg", ".jpg", ".png" })]
        [AllowedMimeTypes(new string[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "image/jpeg", "image/png" })]
        [MaxFileSize(5 * 1024 * 1024)]
        public IFormFile? Attachment { get; set; }
    }
}
