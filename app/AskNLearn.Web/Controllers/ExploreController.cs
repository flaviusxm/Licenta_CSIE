using Microsoft.AspNetCore.Mvc;

namespace AskNLearn.Web.Controllers
{
    public class ExploreController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
