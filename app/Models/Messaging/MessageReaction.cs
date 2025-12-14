using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Models.Core;

namespace AskNLearn.Models.Messaging
{
    [PrimaryKey(nameof(MessageId), nameof(UserId), nameof(EmojiCode))]
    [Table("MessageReactions")]
    public class MessageReaction
    {
        public Guid MessageId { get; set; }

        [ForeignKey(nameof(MessageId))]
        public Message Message { get; set; } = null!;

        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        [MaxLength(50)]
        public string EmojiCode { get; set; } = null!;
    }
}
