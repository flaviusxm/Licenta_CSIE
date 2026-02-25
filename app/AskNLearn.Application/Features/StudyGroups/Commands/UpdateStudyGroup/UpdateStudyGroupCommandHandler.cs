using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.StudyGroups.Commands.UpdateStudyGroup
{
    public class UpdateStudyGroupCommandHandler : IRequestHandler<UpdateStudyGroupCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public UpdateStudyGroupCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateStudyGroupCommand request, CancellationToken cancellationToken)
        {
            var studyGroup = await _context.StudyGroups
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (studyGroup == null)
            {
                return false;
            }

            studyGroup.Name = request.Name;
            studyGroup.Description = request.Description;
            studyGroup.SubjectArea = request.SubjectArea;
            studyGroup.IsPublic = request.IsPublic;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
