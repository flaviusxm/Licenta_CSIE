using AskNLearn.Domain.Entities.Core;
using AskNLearn.Infrastructure.Persistance;
using AskNLearn.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Web.Controllers;

public class LeaderboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(int page = 1, string? searchTerm = null, string? institution = null, string? sortBy = "PointsDesc")
    {
        ViewData["ActivePage"] = "Leaderboard";
        int pageSize = 20;

        var globalTopQuery = context.Users
            .Where(u => u.Role != Role.Admin)
            .OrderByDescending(u => u.ReputationPoints);

        var topUsers = await globalTopQuery.Take(3).ToListAsync();

        var query = context.Users
            .Where(u => u.Role != Role.Admin);

        bool isFiltered = !string.IsNullOrEmpty(searchTerm) || !string.IsNullOrEmpty(institution);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => u.FullName.Contains(searchTerm) || u.UserName.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(institution))
        {
            query = query.Where(u => u.Institution != null && u.Institution.Contains(institution));
        }

        query = sortBy switch
        {
            "PointsAsc" => query.OrderBy(u => u.ReputationPoints),
            "NameAsc" => query.OrderBy(u => u.FullName),
            "NameDesc" => query.OrderByDescending(u => u.FullName),
            _ => query.OrderByDescending(u => u.ReputationPoints)
        };

        int totalUsers = await query.CountAsync();
        
        // If not filtered and on page 1, we skip the top 3. Otherwise we show everything that matches the query.
        var rankingTable = await query.Skip((isFiltered ? 0 : 3) + (page - 1) * pageSize).Take(pageSize).ToListAsync();

        var currentUser = await userManager.GetUserAsync(User);
        int currentUserRank = 0;
        int currentUserPoints = 0;

        if (currentUser != null)
        {
            currentUserPoints = currentUser.ReputationPoints;
            // Efficient way to find rank without loading all users
            currentUserRank = await context.Users.CountAsync(u => u.ReputationPoints > currentUserPoints && u.Role != Role.Admin) + 1;
        }

        var viewModel = new LeaderboardViewModel
        {
            TopUsers = topUsers,
            RankingTable = rankingTable,
            CurrentUserRank = currentUserRank,
            CurrentUserPoints = currentUserPoints,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(Math.Max(0, totalUsers - 3) / (double)pageSize),
            SearchTerm = searchTerm,
            Institution = institution,
            SortBy = sortBy ?? "PointsDesc"
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

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_LeaderboardData", viewModel);
        }

        return View(viewModel);
    }
}
