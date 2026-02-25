using AskNLearn.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.StudyGroups.Commands.DeleteStudyGroup
{
    public class DeleteStudyGroupCommandHandler : IRequestHandler<DeleteStudyGroupCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public DeleteStudyGroupCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteStudyGroupCommand request, CancellationToken cancellationToken)
        {
            var studyGroup = await _context.StudyGroups
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (studyGroup == null)
            {
                return false;
            }

            _context.StudyGroups.Remove(studyGroup);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
