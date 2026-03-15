using AskNLearn.Application.Features.StudyGroups.Commands.UpdateStudyGroup;
using AskNLearn.Application.Features.StudyGroups.Queries;
using System.Collections.Generic;

namespace AskNLearn.Web.Models
{
    public class EditStudyGroupViewModel
    {
        public UpdateStudyGroupCommand Command { get; set; } = null!;
        public List<ChannelDto> Channels { get; set; } = new();
    }
}
