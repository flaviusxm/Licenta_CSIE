using MediatR;
using System.Collections.Generic;

namespace AskNLearn.Application.Features.Users.Commands.SubmitVerificationRequest
{
    public class SubmitVerificationRequestCommand : IRequest<List<string>>
    {
        public string UserId { get; set; } = string.Empty;
        public string StudentIdUrl { get; set; } = string.Empty;
        public string CarnetUrl { get; set; } = string.Empty;
    }
}
