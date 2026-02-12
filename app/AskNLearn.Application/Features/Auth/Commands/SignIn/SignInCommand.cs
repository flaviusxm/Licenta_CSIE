using MediatR;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AskNLearn.Application.Features.Auth.Commands.SignIn
{
    public class SignInCommand : IRequest<List<string>>
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
