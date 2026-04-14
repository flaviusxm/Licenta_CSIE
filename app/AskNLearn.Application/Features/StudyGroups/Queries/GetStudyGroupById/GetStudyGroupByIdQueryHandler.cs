using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Application.Common.Models;
using AskNLearn.Application.Features.StudyGroups.Queries;
using AskNLearn.Domain.Entities.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.StudyGroups.Queries.GetStudyGroupById
{
    public class GetStudyGroupByIdQueryHandler : IRequestHandler<GetStudyGroupByIdQuery, StudyGroupDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetStudyGroupByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StudyGroupDto?> Handle(GetStudyGroupByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.StudyGroups
                .Where(x => x.Id == request.Id)
                .Select(x => new StudyGroupDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    SubjectArea = x.SubjectArea,
                    IsPublic = x.IsPublic,
                    OwnerId = x.OwnerId,
                    OwnerUserName = x.Owner != null ? x.Owner.UserName : null,
                    CreatedAt = x.CreatedAt,
                    MemberCount = x.Members.Count,
                    Channels = x.Channels.OrderBy(c => c.Position).Select(c => new ChannelDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Type = (ChannelType)c.Type,
                        Topic = c.Topic,
                        Position = c.Position
                    }).ToList(),
                    TopMembers = x.Members
                        .Where(m => !m.IsBanned)
                        .OrderByDescending(m => m.UserId == x.OwnerId)
                        .ThenBy(m => m.User.UserName)
                        .Select(m => new MemberDto
                        {
                            UserId = m.UserId,
                            UserName = m.User.UserName ?? m.UserId,
                            FullName = m.User.FullName,
                            AvatarUrl = m.User.AvatarUrl,
                            IsOwner = m.UserId == x.OwnerId,
                            ConnectionStatus = request.CurrentUserId == null ? ConnectionStatus.None : 
                                _context.Friendships.Any(f => (f.RequesterId == request.CurrentUserId && f.AddresseeId == m.UserId && f.Status == FriendshipStatus.Accepted) || 
                                                             (f.RequesterId == m.UserId && f.AddresseeId == request.CurrentUserId && f.Status == FriendshipStatus.Accepted)) ? ConnectionStatus.Accepted :
                                _context.Friendships.Any(f => f.RequesterId == request.CurrentUserId && f.AddresseeId == m.UserId && f.Status == FriendshipStatus.Pending) ? ConnectionStatus.PendingSent :
                                _context.Friendships.Any(f => f.RequesterId == m.UserId && f.AddresseeId == request.CurrentUserId && f.Status == FriendshipStatus.Pending) ? ConnectionStatus.PendingReceived : ConnectionStatus.None
                        }).Take(20).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
