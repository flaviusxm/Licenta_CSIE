using MediatR;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AskNLearn.Application.Features.Auth.Commands.SignIn
{
    public class SignInCommand : IRequest<List<string>>
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(200, ErrorMessage = "Email address cannot exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}