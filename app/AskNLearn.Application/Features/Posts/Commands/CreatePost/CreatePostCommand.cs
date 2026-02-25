using MediatR;
using System;
using System.ComponentModel.DataAnnotations;

namespace AskNLearn.Application.Features.Posts.Commands.CreatePost
{
    public class CreatePostCommand : IRequest<Guid>
    {
        public Guid CommunityId { get; set; }

        public string? AuthorId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;
    }
}
