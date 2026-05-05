using System;
using System.Threading.Tasks;

namespace AskNLearn.Application.Common.Interfaces
{
    public interface IReputationService
    {
        Task AddPointsAsync(string userId, int points);
        Task RemovePointsAsync(string userId, int points);
        Task UpdateUserRankAsync(string userId);
    }
}
