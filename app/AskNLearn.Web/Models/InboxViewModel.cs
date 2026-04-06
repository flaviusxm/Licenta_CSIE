using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Web.Models
{
    public class InboxViewModel
    {
        public List<ConversationPreviewViewModel> RecentMessages { get; set; } = new();
        public List<Notification> RecentNotifications { get; set; } = new();
        public List<Friendship> PendingRequests { get; set; } = new();
        public int TotalConnections { get; set; }
        public string FlowState { get; set; } = "Stable";
        public int TotalUnreadCount { get; set; }
    }
}
