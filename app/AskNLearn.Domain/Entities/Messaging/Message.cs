using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Domain.Entities.Messaging
{
    [Table("Messages")]
    public class Message
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid ConversationId { get; set; }
        [ForeignKey(nameof(ConversationId))]
        public DirectConversation Conversation { get; set; } = null!;

        public string? AuthorId { get; set; }
        [ForeignKey(nameof(AuthorId))]
        public ApplicationUser? Author { get; set; }

        public string? Content { get; set; } 

        public Guid? ReplyToMessageId { get; set; }
        [ForeignKey(nameof(ReplyToMessageId))]
        public Message? ReplyToMessage { get; set; }
        public ICollection<Message> Replies { get; set; } = new List<Message>();

        public bool IsEdited { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;

        public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    }
}
