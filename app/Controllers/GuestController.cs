using Microsoft.AspNetCore.Mvc;

namespace AskNLearn.Controllers.Guest
{
    public class GuestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
