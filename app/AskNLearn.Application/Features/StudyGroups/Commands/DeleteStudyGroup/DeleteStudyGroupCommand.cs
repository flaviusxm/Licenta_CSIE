using MediatR;

namespace AskNLearn.Application.Features.StudyGroups.Commands.DeleteStudyGroup
{
    public class DeleteStudyGroupCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
