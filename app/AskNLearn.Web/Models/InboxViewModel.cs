using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Web.Models
{
    public class InboxViewModel
    {
        public List<ConversationPreviewViewModel> RecentMessages { get; set; } = new();
        public List<Notification> RecentNotifications { get; set; } = new();
        public int TotalUnreadCount { get; set; }
    }
}
