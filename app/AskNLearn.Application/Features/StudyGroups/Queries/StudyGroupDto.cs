using System;

namespace AskNLearn.Application.Features.StudyGroups.Queries
{
    public class StudyGroupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? SubjectArea { get; set; }
        public bool IsPublic { get; set; }
        public string? OwnerId { get; set; }
        public string? OwnerUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
        public List<ChannelDto> Channels { get; set; } = new ();
    }
}
