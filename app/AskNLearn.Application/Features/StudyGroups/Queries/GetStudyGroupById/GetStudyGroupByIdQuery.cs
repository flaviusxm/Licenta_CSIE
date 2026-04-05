using AskNLearn.Application.Features.StudyGroups.Queries;
using MediatR;
using System;

namespace AskNLearn.Application.Features.StudyGroups.Queries.GetStudyGroupById
{
    public class GetStudyGroupByIdQuery : IRequest<StudyGroupDto?>
    {
        public Guid Id { get; set; }
        public string? CurrentUserId { get; set; }
    }
}
