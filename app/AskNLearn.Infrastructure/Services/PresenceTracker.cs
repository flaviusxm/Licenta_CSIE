using AskNLearn.Application.Common.Interfaces;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace AskNLearn.Infrastructure.Services
{
    public class PresenceTracker : IPresenceTracker
    {
        // UserId -> Set of ConnectionIds
        private static readonly ConcurrentDictionary<string, HashSet<string>> OnlineUsers = new();

        public Task<bool> UserConnected(string userId, string connectionId)
        {
            bool isFirstConnection = false;
            OnlineUsers.AddOrUpdate(userId, 
                _ => {
                    isFirstConnection = true;
                    return new HashSet<string> { connectionId };
                }, 
                (_, connections) => {
                    lock(connections)
                    {
                        connections.Add(connectionId);
                    }
                    return connections;
                });

            return Task.FromResult(isFirstConnection);
        }

        public Task<bool> UserDisconnected(string userId, string connectionId)
        {
            bool isLastConnection = false;
            if (OnlineUsers.TryGetValue(userId, out var connections))
            {
                lock(connections)
                {
                    connections.Remove(connectionId);
                    if (connections.Count == 0)
                    {
                        OnlineUsers.TryRemove(userId, out _);
                        isLastConnection = true;
                    }
                }
            }

            return Task.FromResult(isLastConnection);
        }

        public Task<string[]> GetOnlineUsers()
        {
            return Task.FromResult(OnlineUsers.Keys.ToArray());
        }

        public Task<bool> IsUserOnline(string userId)
        {
            return Task.FromResult(OnlineUsers.ContainsKey(userId));
        }

    }
}
