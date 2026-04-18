using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Web.Models
{
    public class InboxViewModel
    {
        public List<ConversationPreviewViewModel> RecentMessages { get; set; } = new();
        public List<ConversationPreviewViewModel> RecentChannels { get; set; } = new();
        public List<Notification> RecentNotifications { get; set; } = new();
        public List<Friendship> PendingRequests { get; set; } = new();
        public int TotalConnections { get; set; }
        public string FlowState { get; set; } = "Stable";
        public int TotalUnreadCount { get; set; }
        public List<AskNLearn.Domain.Entities.Core.ApplicationUser> Connections { get; set; } = new();
        public AskNLearn.Domain.Entities.Messaging.DirectConversation? SelectedConversation { get; set; }
        public AskNLearn.Domain.Entities.StudyGroup.Channel? SelectedChannel { get; set; }
    }
}
