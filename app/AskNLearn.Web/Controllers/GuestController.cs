namespace AskNLearn.Web.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

    public class GuestController:Controller
    {
    private readonly IMediator mediator;
    public GuestController(IMediator mediator) {
        this.mediator = mediator;
    }
    public IActionResult Index()
    {
        return View();
    }

    }

