using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.StudyGroup;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.StudyGroups.Commands.CreateStudyGroup
{
    public class CreateStudyGroupCommandHandler : IRequestHandler<CreateStudyGroupCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public CreateStudyGroupCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateStudyGroupCommand request, CancellationToken cancellationToken)
        {
            var studyGroupId = Guid.NewGuid();
            var studyGroup = new StudyGroup
            {
                Id = studyGroupId,
                Name = request.Name,
                Description = request.Description,
                SubjectArea = request.SubjectArea,
                IsPublic = request.IsPublic,
                OwnerId = request.OwnerId,
                CreatedAt = DateTime.UtcNow
            };

            // Initialize default roles for the group
            var adminRole = new GroupRole { Id = Guid.NewGuid(), GroupId = studyGroupId, Name = "Admin", Permissions = "ALL" };
            var memberRole = new GroupRole { Id = Guid.NewGuid(), GroupId = studyGroupId, Name = "Member", Permissions = "READ,WRITE" };

            // Automatically add creator as the first member with Admin role
            var creatorMembership = new GroupMembership
            {
                GroupId = studyGroupId,
                UserId = request.OwnerId,
                GroupRoleId = adminRole.Id,
                JoinedAt = DateTime.UtcNow
            };

            await _context.StudyGroups.AddAsync(studyGroup, cancellationToken);
            await _context.GroupRoles.AddRangeAsync(new[] { adminRole, memberRole }, cancellationToken);
            await _context.GroupMemberships.AddAsync(creatorMembership, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return studyGroup.Id;
        }
    }
}
