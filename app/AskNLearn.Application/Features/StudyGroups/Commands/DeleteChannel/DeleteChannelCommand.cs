using MediatR;
using System;

namespace AskNLearn.Application.Features.StudyGroups.Commands.DeleteChannel
{
    public class DeleteChannelCommand : IRequest<bool>
    {
        public Guid Id { get; set; }
    }
}
