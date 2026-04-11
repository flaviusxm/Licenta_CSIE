using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AskNLearn.Domain.Entities.Core;
using AskNLearn.Application.Common.Interfaces;

namespace AskNLearn.Application.Features.Auth.Commands.SignIn
{
    public class SignInCommandHandler : IRequestHandler<SignInCommand, SignInResponse>
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtService _jwtService;

        public SignInCommandHandler(
            SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userManager,
            IJwtService jwtService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _jwtService = jwtService;
        }

        public async Task<SignInResponse> Handle(SignInCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new SignInResponse { Errors = new List<string> { "Invalid login attempt." } };
            }

            var result = await _signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var token = _jwtService.GenerateToken(user);
                return new SignInResponse 
                { 
                    Succeeded = true, 
                    Token = token 
                };
            }
            
            if (result.IsLockedOut)
            {
                return new SignInResponse { IsLockedOut = true, Errors = new List<string> { "User account locked out." } };
            }

            if (result.IsNotAllowed)
            {
                return new SignInResponse { Errors = new List<string> { "Please confirm your email before logging in." } };
            }

            return new SignInResponse { Errors = new List<string> { "Invalid login attempt." } };
        }
    }
}
