using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Application.Features.StudyGroups.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.StudyGroups.Queries.GetStudyGroups
{
    public class GetStudyGroupsQueryHandler : IRequestHandler<GetStudyGroupsQuery, List<StudyGroupDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetStudyGroupsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<StudyGroupDto>> Handle(GetStudyGroupsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.StudyGroups.AsQueryable();

            if (request.OnlyPublic)
            {
                query = query.Where(x => x.IsPublic);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(x => x.Name.Contains(request.SearchTerm) || (x.Description != null && x.Description.Contains(request.SearchTerm)));
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
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
                .ToListAsync(cancellationToken);
        }
    }
}
