using AskNLearn.Application.Features.Auth.Queries.GetSignIn;
using AskNLearn.Application.Features.Auth.Queries.GetSignUp;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AskNLearn.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IMediator mediator;

        public AuthController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<IActionResult> SignIn()
        {
            await mediator.Send(new GetSignInQuery());
            return View();
        }

        public async Task<IActionResult> SignUp()
        {
            await mediator.Send(new GetSignUpQuery());
            return View();
        }
    }
}
