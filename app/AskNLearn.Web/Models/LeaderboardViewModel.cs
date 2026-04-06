using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Web.Models
{
    public class LeaderboardViewModel
    {
        public List<ApplicationUser> TopUsers { get; set; } = new();
        public List<ApplicationUser> RankingTable { get; set; } = new();
        public int CurrentUserRank { get; set; }
        public int CurrentUserPoints { get; set; }
        public string CurrentUserLeague { get; set; } = "Bronze";
        public int PointsToNextLeague { get; set; }
        public string NextLeagueName { get; set; } = string.Empty;
        public int ProgressToNextLeague { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public string? SearchTerm { get; set; }
        public string? Institution { get; set; }
        public string? SortBy { get; set; } = "PointsDesc";
    }

    public static class LeaderboardExtensions
    {
        public static string GetLeague(int points) => points switch
        {
            >= 5000 => "Grandmaster",
            >= 2500 => "Master",
            >= 1000 => "Diamond",
            >= 500 => "Gold",
            >= 200 => "Silver",
            _ => "Bronze"
        };

        public static (string Name, int Threshold) GetNextLeague(int points) => points switch
        {
            < 200 => ("Silver", 200),
            < 500 => ("Gold", 500),
            < 1000 => ("Diamond", 1000),
            < 2500 => ("Master", 2500),
            < 5000 => ("Grandmaster", 5000),
            _ => ("Legend", 10000)
        };
    }
}
