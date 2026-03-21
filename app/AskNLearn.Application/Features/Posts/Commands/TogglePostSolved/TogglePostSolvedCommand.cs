using MediatR;
using System;

namespace AskNLearn.Application.Features.Posts.Commands.TogglePostSolved
{
    public class TogglePostSolvedCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
    }
}
