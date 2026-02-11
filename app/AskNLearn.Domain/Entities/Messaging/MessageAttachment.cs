using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Domain.Entities.Messaging
{
    [PrimaryKey(nameof(MessageId), nameof(FileId))]
    [Table("MessageAttachments")]
    public class MessageAttachment
    {
        public Guid MessageId { get; set; }

        [ForeignKey(nameof(MessageId))]
        public Message Message { get; set; } = null!;

        public Guid FileId { get; set; }

        [ForeignKey(nameof(FileId))]
        public StoredFile File { get; set; } = null!;
    }
}
