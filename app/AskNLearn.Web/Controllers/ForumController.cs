using Microsoft.AspNetCore.Mvc;

namespace AskNLearn.Web.Controllers
{
    public class ForumController : Controller
    {
        public IActionResult Details(string id)
        {
            ViewData["ForumId"] = id;
            return View();
        }
    }
}
