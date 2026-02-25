using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AskNLearn.Domain.Entities.Core;

namespace AskNLearn.Application.Features.Auth.Commands.SignIn
{
    public class SignInCommandHandler : IRequestHandler<SignInCommand, List<string>>
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public SignInCommandHandler(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task<List<string>> Handle(SignInCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new List<string> { "Invalid login attempt." };
            }

            var result = await _signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return new List<string>();
            }
            
            if (result.IsLockedOut)
            {
                return new List<string> { "User account locked out." };
            }

            return new List<string> { "Invalid login attempt." };
        }
    }
}
