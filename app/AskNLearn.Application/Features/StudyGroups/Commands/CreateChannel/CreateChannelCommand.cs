using AskNLearn.Domain.Entities.StudyGroup;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AskNLearn.Application.Features.StudyGroups.Commands.CreateChannel
{
    public class CreateChannelCommand : IRequest<Guid>
    {
        public Guid GroupId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        public ChannelType Type { get; set; } = ChannelType.Text;

        [MaxLength(255)]
        public string? Topic { get; set; }
    }
}
