using AskNLearn.Application.Features.Auth.Commands.SignIn;
using AskNLearn.Application.Features.Auth.Commands.SignUp;
using AskNLearn.Application.Features.Auth.Queries.GetSignIn;
using AskNLearn.Application.Features.Auth.Commands.SignOut;
using AskNLearn.Application.Features.Auth.Queries.GetSignUp;
using AskNLearn.Domain.Entities.Core;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;
namespace AskNLearn.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IMediator mediator;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(IMediator mediator, UserManager<ApplicationUser> userManager)
        {
            this.mediator = mediator;
            _userManager = userManager;
        }

        public async Task<IActionResult> SignIn(string? returnUrl = null)
        {
            await mediator.Send(new GetSignInQuery());
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

       
        public async Task<IActionResult> SignUp()
        {
            await mediator.Send(new GetSignUpQuery());
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignIn(SignInCommand command, string? returnUrl = null)
        {
             if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(command);
            }

            Log.Information("Attempting sign-in for user: {Email}", command.Email);
            var errors = await mediator.Send(command);

            if (errors.Count == 0)
            {
                var user = await _userManager.FindByEmailAsync(command.Email);
                if (user != null)
                {
                    Log.Information("User {Email} signed in successfully. Role: {Role}", command.Email, user.Role);
                    if (user.Role == Role.Admin)
                    {
                        Log.Information("Redirecting admin user to Admin dashboard.");
                        return RedirectToAction("Index", "Admin");
                    }
                }
                else
                {
                    Log.Warning("User not found after successful sign-in: {Email}", command.Email);
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    Log.Information("Redirecting to ReturnUrl: {ReturnUrl}", returnUrl);
                    return Redirect(returnUrl);
                }
                Log.Information("Redirecting to Home Index.");
                return RedirectToAction("Index", "Home");
            }

            Log.Warning("Sign-in failed for user {Email}. Errors: {Errors}", command.Email, string.Join(", ", errors));
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(command);
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpCommand command)
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
        public new async Task<IActionResult> SignOut()
        {
            await mediator.Send(new SignOutCommand());
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
