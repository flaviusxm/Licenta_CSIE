using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Domain.Entities.SocialFeed
{
    [PrimaryKey(nameof(PostId), nameof(UserId))]
    [Table("PostVotes")]
    public class PostVote
    {
        public Guid PostId { get; set; }

        [ForeignKey(nameof(PostId))]
        public Post Post { get; set; } = null!;

        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        public short VoteValue { get; set; } 
    }
}
