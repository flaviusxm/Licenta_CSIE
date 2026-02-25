using AskNLearn.Application.Features.Communities.Queries.GetCommunities;
using MediatR;
using System;

namespace AskNLearn.Application.Features.Communities.Queries.GetCommunityById
{
    public class GetCommunityByIdQuery : IRequest<CommunityDto?>
    {
        public Guid Id { get; set; }
    }
}
