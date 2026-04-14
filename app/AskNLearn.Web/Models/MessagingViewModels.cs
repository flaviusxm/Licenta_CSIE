using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Web.Models
{
    public class ConversationPreviewViewModel
    {
        public Guid ConversationId { get; set; }
        public string OtherUserId { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public string OtherUserAvatar { get; set; } = string.Empty;
        public string LastMessageContent { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public bool IsUnread { get; set; }
        public int UnreadCount { get; set; }
    }

    public class DirectMessagesDropdownViewModel
    {
        public List<ConversationPreviewViewModel> RecentConversations { get; set; } = new();
        public int UnreadCount { get; set; }
    }
}
