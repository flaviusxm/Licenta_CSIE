using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Application.Features.StudyGroups.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.StudyGroups.Queries.GetStudyGroupById
{
    public class GetStudyGroupByIdQueryHandler : IRequestHandler<GetStudyGroupByIdQuery, StudyGroupDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetStudyGroupByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StudyGroupDto?> Handle(GetStudyGroupByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.StudyGroups
                .Where(x => x.Id == request.Id)
                .Select(x => new StudyGroupDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    SubjectArea = x.SubjectArea,
                    IsPublic = x.IsPublic,
                    OwnerId = x.OwnerId,
                    OwnerUserName = x.Owner != null ? x.Owner.UserName : null,
                    CreatedAt = x.CreatedAt,
                    MemberCount = x.Members.Count
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
