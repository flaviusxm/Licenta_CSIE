using MediatR;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AskNLearn.Application.Features.Auth.Commands.SignUp
{
    public class SignUpCommand : IRequest<List<string>>
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MinLength(3, ErrorMessage = "Full name must contain at least 3 characters.")]
        [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(200, ErrorMessage = "Email address cannot exceed 200 characters.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must contain at least 8 characters.")]
        [MaxLength(100)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter and one number.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = null!;

        public string? VerificationBaseUrl { get; set; }
    }
}