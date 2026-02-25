using MediatR;
using System;

namespace AskNLearn.Application.Features.Communities.Commands.DeleteCommunity
{
    public class DeleteCommunityCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
