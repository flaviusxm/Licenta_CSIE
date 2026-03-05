using MediatR;
using System;
using System.ComponentModel.DataAnnotations;

namespace AskNLearn.Application.Features.Communities.Commands.UpdateCommunity
{
    public class UpdateCommunityCommand : IRequest<bool>
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? Image { get; set; }

    }
}
