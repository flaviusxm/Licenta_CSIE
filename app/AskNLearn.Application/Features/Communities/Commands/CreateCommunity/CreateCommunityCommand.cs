using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AskNLearn.Application.Features.Communities.Commands.CreateCommunity
{
    public class CreateCommunityCommand : IRequest<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? Slug { get; set; }

        public string? Description { get; set; }

        public string? CreatorId { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? Image { get; set; }

    }
}
