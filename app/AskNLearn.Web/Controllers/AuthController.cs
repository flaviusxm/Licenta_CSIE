using AskNLearn.Application.Features.Auth.Commands.SignIn;
using AskNLearn.Application.Features.Auth.Commands.SignUp;
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

        [HttpPost]
        public async Task<IActionResult> SignIn(SignInCommand command)
        {
             if (!ModelState.IsValid)
            {
                return View(command);
            }

            var errors = await mediator.Send(command);

            if (errors.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(command);
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpCommand command)
        {
            // 1. Validare formular HTML (Required, Email format etc.)
            if (!ModelState.IsValid)
            {
                return View(command);
            }

            // 2. Trimitem Comanda la Application Layer prin MediatR
            var errors = await mediator.Send(command);

            // 3. Verificăm rezultatul
            if (errors.Count == 0)
            {
                // Succes -> Mergem la prima pagină
                return RedirectToAction("Index", "Home");
            }

            // 4. Dacă avem erori de la Identity (ex: Email existent), le afișăm
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(command);
        }

    }
}
