using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AskNLearn.Application.Features.StudyGroups.Commands.CreateStudyGroup
{
    public class CreateStudyGroupCommand : IRequest<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string? SubjectArea { get; set; }

        public bool IsPublic { get; set; } = false;

        public string? OwnerId { get; set; }
    }
}
