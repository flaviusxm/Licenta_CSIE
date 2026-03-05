using System;

namespace AskNLearn.Application.Features.Communities.Queries.GetCommunities
{
    public class CommunityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? CreatorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
        public int PostCount { get; set; }
    }
}
