using MediatR;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AskNLearn.Application.Features.Posts.Commands.AddComment
{
    public class AddCommentCommand : IRequest<Guid>
    {
        public Guid PostId { get; set; }
        public Guid? CommunityId { get; set; }
        public Guid? ReplyToMessageId { get; set; }
        public string? AuthorId { get; set; }

        public string? Content { get; set; }

        public IFormFile? Attachment { get; set; }
    }
}
