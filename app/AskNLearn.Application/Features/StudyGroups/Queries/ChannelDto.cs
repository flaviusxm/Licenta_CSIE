using System;

namespace AskNLearn.Application.Features.StudyGroups.Queries
{
    public enum ChannelType
    {
        Text,
        Voice,
        Video
    }

    public class ChannelDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public ChannelType Type { get; set; }
        public string? Topic { get; set; }
        public int Position { get; set; }
    }
}
