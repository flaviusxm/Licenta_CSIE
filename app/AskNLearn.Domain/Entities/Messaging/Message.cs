using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AskNLearn.Domain.Entities.Core;
using AskNLearn.Domain.Entities.SocialFeed;
using AskNLearn.Domain.Entities.StudyGroup;

namespace AskNLearn.Domain.Entities.Messaging
{
    [Table("Messages")]
    public class Message
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();


        public Guid? ChannelId { get; set; }
        [ForeignKey(nameof(ChannelId))]
        public Channel? Channel { get; set; }

        public Guid? ConversationId { get; set; }
        [ForeignKey(nameof(ConversationId))]
        public DirectConversation? Conversation { get; set; }

        public Guid? PostId { get; set; }
        [ForeignKey(nameof(PostId))]
        public Post? Post { get; set; }


        public string? AuthorId { get; set; }
        [ForeignKey(nameof(AuthorId))]
        public ApplicationUser? Author { get; set; }

        public string? Content { get; set; } 

        public Guid? ReplyToMessageId { get; set; }
        [ForeignKey(nameof(ReplyToMessageId))]
        public Message? ReplyToMessage { get; set; }
        public ICollection<Message> Replies { get; set; } = new List<Message>();

        public bool IsPinned { get; set; } = false;
        public bool IsEdited { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;
        public string? ModerationReason { get; set; }

        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
        public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    }
}
