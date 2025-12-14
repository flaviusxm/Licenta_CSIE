using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskNLearn.Models.Messaging
{
    [Table("DirectConversations")]
    public class DirectConversation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<DirectConversationParticipant> Participants { get; set; } = new List<DirectConversationParticipant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
