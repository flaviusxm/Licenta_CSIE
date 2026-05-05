using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Domain.Entities.SocialFeed
{
    [Table("PostTags")]
    public class PostTag
    {
        public Guid PostId { get; set; }
        
        [ForeignKey(nameof(PostId))]
        public Post Post { get; set; } = null!;

        public Guid TagId { get; set; }
        
        [ForeignKey(nameof(TagId))]
        public Tag Tag { get; set; } = null!;
    }
}
