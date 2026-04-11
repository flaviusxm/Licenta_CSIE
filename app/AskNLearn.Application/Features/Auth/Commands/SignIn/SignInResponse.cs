using System.Collections.Generic;

namespace AskNLearn.Application.Features.Auth.Commands.SignIn
{
    public class SignInResponse
    {
        public bool Succeeded { get; set; }
        public string? Token { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public bool IsLockedOut { get; set; }
    }
}
