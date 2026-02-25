using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.StudyGroup;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.StudyGroups.Commands.CreateStudyGroup
{
    public class CreateStudyGroupCommandHandler : IRequestHandler<CreateStudyGroupCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public CreateStudyGroupCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateStudyGroupCommand request, CancellationToken cancellationToken)
        {
            var studyGroup = new StudyGroup
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                SubjectArea = request.SubjectArea,
                IsPublic = request.IsPublic,
                OwnerId = request.OwnerId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.StudyGroups.AddAsync(studyGroup, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return studyGroup.Id;
        }
    }
}
