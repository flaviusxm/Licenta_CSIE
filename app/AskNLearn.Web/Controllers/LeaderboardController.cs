using AskNLearn.Domain.Entities.Core;
using AskNLearn.Infrastructure.Persistance;
using AskNLearn.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Web.Controllers;

public class LeaderboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["ActivePage"] = "Leaderboard";

        var users = await context.Users
            .OrderByDescending(u => u.ReputationPoints)
            .ToListAsync();

        var topUsers = users.Take(3).ToList();
        var rankingTable = users.Skip(3).ToList();

        var currentUser = await userManager.GetUserAsync(User);
        int currentUserRank = 0;
        int currentUserPoints = 0;

        if (currentUser != null)
        {
            currentUserRank = users.FindIndex(u => u.Id == currentUser.Id) + 1;
            currentUserPoints = currentUser.ReputationPoints;
        }

        var viewModel = new LeaderboardViewModel
        {
            TopUsers = topUsers,
            RankingTable = rankingTable,
            CurrentUserRank = currentUserRank,
            CurrentUserPoints = currentUserPoints
        };

        if (currentUser != null)
        {
            viewModel.CurrentUserLeague = LeaderboardExtensions.GetLeague(currentUserPoints);
            var (nextName, nextThreshold) = LeaderboardExtensions.GetNextLeague(currentUserPoints);
            viewModel.NextLeagueName = nextName;
            viewModel.PointsToNextLeague = nextThreshold - currentUserPoints;
            
            // Calculate progress percentage
            var currentThreshold = LeaderboardExtensions.GetLeague(currentUserPoints) switch {
                "Grandmaster" => 5000,
                "Master" => 2500,
                "Diamond" => 1000,
                "Gold" => 500,
                "Silver" => 200,
                _ => 0
            };
            
            int range = nextThreshold - currentThreshold;
            int progress = ((currentUserPoints - currentThreshold) * 100) / (range > 0 ? range : 1);
            viewModel.ProgressToNextLeague = Math.Clamp(progress, 0, 100);
        }

        return View(viewModel);
    }
}
