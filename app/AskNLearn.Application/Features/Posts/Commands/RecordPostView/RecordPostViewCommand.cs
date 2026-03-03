using MediatR;
using System;

namespace AskNLearn.Application.Features.Posts.Commands.RecordPostView
{
    public class RecordPostViewCommand : IRequest<bool>
    {
        public Guid PostId { get; set; }
        public string UserId { get; set; } = null!;
    }
}
