using MediatR;
using System.ComponentModel.DataAnnotations;

namespace AskNLearn.Application.Features.StudyGroups.Commands.UpdateStudyGroup
{
    public class UpdateStudyGroupCommand : IRequest<bool>
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string? SubjectArea { get; set; }

        public bool IsPublic { get; set; }
    }
}
