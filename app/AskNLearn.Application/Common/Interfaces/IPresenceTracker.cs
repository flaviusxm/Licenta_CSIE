using System.Collections.Generic;
using System.Threading.Tasks;

namespace AskNLearn.Application.Common.Interfaces
{
    public interface IPresenceTracker
    {
        Task<bool> UserConnected(string userId, string connectionId);
        Task<bool> UserDisconnected(string userId, string connectionId);
        Task<string[]> GetOnlineUsers();
        Task<bool> IsUserOnline(string userId);
    }
}
