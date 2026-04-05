using AskNLearn.Application.Common.Models;

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
        public bool IsMember { get; set; }
        public List<ChannelDto> Channels { get; set; } = new ();
        public List<MemberDto> Members { get; set; } = new ();
    }

    public class MemberDto
    {
        public string Id { get; set; } = null!;
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public bool IsOwner { get; set; }
        public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.None;
    }
}
