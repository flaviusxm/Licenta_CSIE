using MediatR;
using Microsoft.AspNetCore.Identity;
using AskNLearn.Domain.Entities.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AskNLearn.Application.Common.Interfaces;

namespace AskNLearn.Application.Features.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, List<string>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileService _fileService;

        public UpdateUserProfileCommandHandler(UserManager<ApplicationUser> userManager, IFileService fileService)
        {
            _userManager = userManager;
            _fileService = fileService;
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

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                user.FullName = request.FullName;
            }
            user.Bio = request.Bio;
            user.Occupation = request.Occupation;
            user.Institution = request.Institution;
            user.Interests = request.Interests;
            user.SocialLinks = request.SocialLinks;

            if (request.AvatarFile != null)
            {
                user.AvatarUrl = await _fileService.UploadFileAsync(request.AvatarFile.OpenReadStream(), request.AvatarFile.FileName, "avatars");
            }
            else if (request.AvatarUrl != null) 
            {
                user.AvatarUrl = request.AvatarUrl;
            }

            if (request.BannerFile != null)
            {
                user.BannerUrl = await _fileService.UploadFileAsync(request.BannerFile.OpenReadStream(), request.BannerFile.FileName, "banners");
            }
            else if (request.BannerUrl != null)
            {
                user.BannerUrl = request.BannerUrl;
            }
            
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
