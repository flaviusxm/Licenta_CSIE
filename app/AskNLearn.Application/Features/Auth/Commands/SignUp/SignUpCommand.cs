using MediatR;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AskNLearn.Application.Features.Auth.Commands.SignUp
{
    public class SignUpCommand : IRequest<List<string>>
    {
        [Required]
        public string FullName { get; set; } 

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }
    }
}
