using Microsoft.AspNetCore.Mvc;

namespace AskNLearn.Web.Controllers;

public class LeaderboardController : Controller
{
    public IActionResult Index()
    {
        ViewData["ActivePage"] = "Leaderboard";
        return View();
    }
}
