using MediatR;
using Microsoft.AspNetCore.Identity;
using AskNLearn.Domain.Entities.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, List<string>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UpdateUserProfileCommandHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<List<string>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.Id);
            var errors = new List<string>();

            if (user == null)
            {
                errors.Add("User not found.");
                return errors;
            }

            user.FullName = request.FullName;
            user.Bio = request.Bio;
            user.AvatarUrl = request.AvatarUrl;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    errors.Add(error.Description);
                }
            }

            return errors;
        }
    }
}
