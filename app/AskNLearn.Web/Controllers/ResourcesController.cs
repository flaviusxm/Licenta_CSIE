using Microsoft.AspNetCore.Mvc;

namespace AskNLearn.Web.Controllers;

public class ResourcesController : Controller
{
    public IActionResult Index()
    {
        ViewData["ActivePage"] = "Resources";
        return View();
    }
}
