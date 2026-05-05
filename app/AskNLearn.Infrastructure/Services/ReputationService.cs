using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Gamification;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AskNLearn.Infrastructure.Services
{
    public class ReputationService : IReputationService
    {
        private readonly IApplicationDbContext _context;

        public ReputationService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddPointsAsync(string userId, int points)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.ReputationPoints += points;
                await UpdateUserRankAsync(userId, user);
                await _context.SaveChangesAsync(default);
            }
        }

        public async Task RemovePointsAsync(string userId, int points)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.ReputationPoints = Math.Max(0, user.ReputationPoints - points);
                await UpdateUserRankAsync(userId, user);
                await _context.SaveChangesAsync(default);
            }
        }

        public async Task UpdateUserRankAsync(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                await UpdateUserRankAsync(userId, user);
                await _context.SaveChangesAsync(default);
            }
        }

        private async Task UpdateUserRankAsync(string userId, AskNLearn.Domain.Entities.Core.ApplicationUser user)
        {
            var ranks = await _context.UserRanks
                .OrderByDescending(r => r.MinPoints)
                .ToListAsync();

            var newRank = ranks.FirstOrDefault(r => user.ReputationPoints >= r.MinPoints);
            
            if (newRank != null && user.CurrentRankId != newRank.Id)
            {
                user.CurrentRankId = newRank.Id;
            }
        }
    }
}
