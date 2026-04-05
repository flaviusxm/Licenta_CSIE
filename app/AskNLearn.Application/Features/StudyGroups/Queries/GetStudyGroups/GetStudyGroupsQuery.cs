using AskNLearn.Application.Features.StudyGroups.Queries;
using MediatR;
using System.Collections.Generic;


namespace AskNLearn.Application.Features.StudyGroups.Queries.GetStudyGroups
{
    public class GetStudyGroupsQuery : IRequest<List<StudyGroupDto>>
    {
        public string? SearchTerm { get; set; }
        public bool OnlyPublic { get; set; } = true;
        public string? CurrentUserId { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 10;
    }
}
