using MediatR;
using System;
using System.ComponentModel.DataAnnotations;

namespace AskNLearn.Application.Features.Posts.Commands.UpdatePost
{
    public class UpdatePostCommand : IRequest<bool>
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        public bool IsSolved { get; set; }
    }
}
