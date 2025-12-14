using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Models.Core; // For ApplicationUser

namespace AskNLearn.Models.Messaging
{
    [PrimaryKey(nameof(ConversationId), nameof(UserId))]
    [Table("DirectConversationParticipants")]
    public class DirectConversationParticipant
    {
        public Guid ConversationId { get; set; }

        [ForeignKey(nameof(ConversationId))]
        public DirectConversation Conversation { get; set; } = null!;

        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        public Guid? LastReadMessageId { get; set; }
    }
}
