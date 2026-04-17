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
    [Route("identity/auth")]
    public class AuthController : Controller
    {
        private readonly IMediator mediator;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(IMediator mediator, UserManager<ApplicationUser> userManager)
        {
            this.mediator = mediator;
            _userManager = userManager;
        }

        [HttpGet("authenticate")]
        public async Task<IActionResult> SignIn(string? returnUrl = null)
        {
            await mediator.Send(new GetSignInQuery());
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

       
        [HttpGet("register")]
        public async Task<IActionResult> SignUp()
        {
            await mediator.Send(new GetSignUpQuery());
            return View();
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> SignIn(SignInCommand command, string? returnUrl = null)
        {
             if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(command);
            }

            Log.Information("Attempting sign-in for user: {Email}", command.Email);
            var response = await mediator.Send(command);

            if (response.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(command.Email);
                if (user != null)
                {
                    Log.Information("User {Email} signed in successfully. Role: {Role}", command.Email, user.Role);
                    
                    // We also store the JWT in a cookie so frontend can use it if needed
                    Response.Cookies.Append("JwtToken", response.Token!, new CookieOptions 
                    { 
                        HttpOnly = true, 
                        Secure = true, 
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddDays(7)
                    });

                    if (user.Role == Role.Admin)
                    {
                        Log.Information("Redirecting admin user to Admin dashboard.");
                        return RedirectToAction("Index", "Admin");
                    }
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    Log.Information("Redirecting to ReturnUrl: {ReturnUrl}", returnUrl);
                    return Redirect(returnUrl);
                }
                Log.Information("Redirecting to Home Index.");
                return RedirectToAction("Index", "Home");
            }

            Log.Warning("Sign-in failed for user {Email}. Errors: {Errors}", command.Email, string.Join(", ", response.Errors));
            foreach (var error in response.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(command);
        }

        [HttpPost("register")]
        public async Task<IActionResult> SignUp(SignUpCommand command)
        {
            if (!ModelState.IsValid)
            {
                return View(command);
            }

            command.VerificationBaseUrl = Url.Action("ConfirmEmail", "Auth", null, Request.Scheme);
            var errors = await mediator.Send(command);

            if (errors.Count == 0)
            {
                return RedirectToAction("VerifyEmailNotice");
            }

            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(command);
        }

        [HttpPost("terminate")]
        public new async Task<IActionResult> SignOut()
        {
            await mediator.Send(new SignOutCommand());
            Response.Cookies.Delete("JwtToken");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult VerifyEmailNotice()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            var decodedToken = System.Text.Encoding.UTF8.GetString(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            
            ViewBag.Status = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email.";
            return View();
        }
    }
}
