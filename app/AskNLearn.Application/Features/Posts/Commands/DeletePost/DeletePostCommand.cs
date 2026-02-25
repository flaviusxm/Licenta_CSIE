using MediatR;
using System;

namespace AskNLearn.Application.Features.Posts.Commands.DeletePost
{
    public class DeletePostCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
