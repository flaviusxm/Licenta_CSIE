using MediatR;
using Microsoft.AspNetCore.Identity;
using AskNLearn.Domain.Entities.Core;
using AskNLearn.Application.Common.Interfaces;

namespace AskNLearn.Application.Features.Auth.Commands.SignUp
{
    public class SignUpCommandHandler : IRequestHandler<SignUpCommand, List<string>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public SignUpCommandHandler(UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<List<string>> Handle(SignUpCommand request, CancellationToken cancellationToken)
        {
            var user = new ApplicationUser 
            { 
                UserName = request.Email, 
                Email = request.Email, 
                FullName = request.FullName,
                EmailConfirmed = false 
            };
            
            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
                
                var verificationLink = $"{request.VerificationBaseUrl}?userId={user.Id}&token={encodedToken}";

                var emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                        <h2 style='color: #4A90E2;'>Welcome to AskNLearn!</h2>
                        <p>Hi {request.FullName},</p>
                        <p>Thank you for joining our community. To get started, please verify your email address by clicking the button below:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{verificationLink}' style='background-color: #4A90E2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Verify Email Address</a>
                        </div>
                        <p>If the button doesn't work, you can also copy and paste this link into your browser:</p>
                        <p style='word-break: break-all; color: #888;'>{verificationLink}</p>
                        <hr style='margin: 20px 0; border: 0; border-top: 1px solid #eee;' />
                        <p style='font-size: 12px; color: #888;'>If you didn't create an account, you can safely ignore this email.</p>
                    </div>";

                await _emailService.SendEmailAsync(user.Email!, "Verify your AskNLearn Account", emailBody);

                return new List<string>();
            }
            return result.Errors.Select(e => e.Description).ToList();
        }
    }
}