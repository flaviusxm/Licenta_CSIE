using AskNLearn.Application.Common.Interfaces;
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
        private readonly IEmailService _emailService;

        public AuthController(IMediator mediator, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            this.mediator = mediator;
            _userManager = userManager;
            _emailService = emailService;
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
                    var cookieOptions = new CookieOptions 
                    { 
                        HttpOnly = true, 
                        Secure = true, 
                        SameSite = SameSiteMode.Strict
                    };

                    if (command.RememberMe)
                    {
                        cookieOptions.Expires = DateTime.UtcNow.AddDays(14);
                    }

                    Response.Cookies.Append("JwtToken", response.Token!, cookieOptions);

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

        [HttpGet("access-denied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet("verify-email-notice")]
        public IActionResult VerifyEmailNotice()
        {
            return View();
        }

        [HttpGet("confirm-email")]
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

            if (user.EmailConfirmed)
            {
                ViewBag.Status = "Your email is already confirmed. Thank you!";
                return View();
            }

            var decodedToken = System.Text.Encoding.UTF8.GetString(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            
            ViewBag.Status = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email. The link might be expired.";
            return View();
        }

        [ViewData]
        public string? Status { get; set; }

        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
            
            var resetLink = Url.Action("ResetPassword", "Auth", new { userId = user.Id, token = encodedToken }, Request.Scheme);

            var emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px; background-color: #ffffff; color: #333;'>
                    <h2 style='color: #007aff;'>Reset Your AskNLearn Password</h2>
                    <p>Hi {user.FullName},</p>
                    <p>We received a request to reset your password. Click the button below to choose a new one:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetLink}' style='background-color: #007aff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold;'>Reset Password</a>
                    </div>
                    <p>If the button doesn't work, copy and paste this link:</p>
                    <p style='word-break: break-all; color: #888;'>{resetLink}</p>
                    <hr style='margin: 20px 0; border: 0; border-top: 1px solid #eee;' />
                    <p style='font-size: 12px; color: #888;'>If you didn't request this, you can ignore this email.</p>
                </div>";

            await _emailService.SendEmailAsync(user.Email!, "Reset Password - AskNLearn", emailBody);

            return Ok(new { message = "Reset link sent to your email." });
        }

        [HttpGet("reset-password")]
        public IActionResult ResetPassword(string userId, string token)
        {
            ViewBag.UserId = userId;
            ViewBag.Token = token;
            return View();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(string userId, string token, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var decodedToken = System.Text.Encoding.UTF8.GetString(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);

            if (result.Succeeded)
            {
                ViewBag.Status = "Password reset successfully. You can now log in.";
                return RedirectToAction("SignIn");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.UserId = userId;
            ViewBag.Token = token;
            return View();
        }
    }
}
