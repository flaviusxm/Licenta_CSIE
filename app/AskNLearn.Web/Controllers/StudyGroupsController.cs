using AskNLearn.Domain.Entities.Core;
using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.StudyGroup;
using AskNLearn.Application.Features.StudyGroups.Commands.CreateChannel;
using AskNLearn.Application.Features.StudyGroups.Commands.CreateStudyGroup;
using AskNLearn.Application.Features.StudyGroups.Commands.DeleteChannel;
using AskNLearn.Application.Features.StudyGroups.Commands.DeleteStudyGroup;
using AskNLearn.Application.Features.StudyGroups.Commands.UpdateStudyGroup;
using AskNLearn.Application.Features.StudyGroups.Queries;
using AskNLearn.Application.Features.StudyGroups.Queries.GetStudyGroupById;
using AskNLearn.Application.Features.StudyGroups.Queries.GetStudyGroups;
using AskNLearn.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    [Route("collaboration/study-groups")]
    public class StudyGroupsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPresenceTracker _presenceTracker;

        public StudyGroupsController(IMediator mediator, IApplicationDbContext context, UserManager<ApplicationUser> userManager, IPresenceTracker presenceTracker)
        {
            _mediator = mediator;
            _context = context;
            _userManager = userManager;
            _presenceTracker = presenceTracker;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchTerm)
        {
            var currentUserId = _userManager.GetUserId(User);
            var groups = await _mediator.Send(new GetStudyGroupsQuery 
            { 
                SearchTerm = searchTerm,
                CurrentUserId = currentUserId,
                Skip = 0,
                Take = 8
            });
            return View(groups);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetGroups(int skip, string? searchTerm)
        {
            var currentUserId = _userManager.GetUserId(User);
            var groups = await _mediator.Send(new GetStudyGroupsQuery 
            { 
                SearchTerm = searchTerm,
                CurrentUserId = currentUserId,
                Skip = skip,
                Take = 8
            });
            return PartialView("_GroupCards", groups);
        }

        [HttpPost]
        public async Task<IActionResult> Join(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // 1. Verify group existence to prevent FK conflict crash
            var groupExists = await _context.StudyGroups.AnyAsync(g => g.Id == id);
            if (!groupExists) return NotFound();

            var alreadyMember = await _context.GroupMemberships.AnyAsync(m => m.GroupId == id && m.UserId == userId);
            if (alreadyMember) return Ok();

            // 2. Locate or create "Member" role
            var memberRole = await _context.GroupRoles.FirstOrDefaultAsync(r => r.GroupId == id && r.Name == "Member");
            
            if (memberRole == null)
            {
                // Safety fallback: if roles weren't created during group creation (legacy data or race condition)
                var adminRole = new GroupRole { Id = Guid.NewGuid(), GroupId = id, Name = "Admin", Permissions = "ALL" };
                memberRole = new GroupRole { Id = Guid.NewGuid(), GroupId = id, Name = "Member", Permissions = "READ,WRITE" };
                _context.GroupRoles.AddRange(adminRole, memberRole);
                await _context.SaveChangesAsync(default);
            }

            var membership = new GroupMembership
            {
                GroupId = id,
                UserId = userId,
                GroupRoleId = memberRole.Id,
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupMemberships.Add(membership);
            await _context.SaveChangesAsync(default);

            return Ok();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUserId = _userManager.GetUserId(User);
            var studyGroup = await _mediator.Send(new AskNLearn.Application.Features.StudyGroups.Queries.GetStudyGroupById.GetStudyGroupByIdQuery { Id = id, CurrentUserId = currentUserId });
            if (studyGroup == null) return NotFound();

            ViewBag.OnlineUsers = await _presenceTracker.GetOnlineUsers();

            return View(studyGroup);
        }

        [AllowAnonymous]
        public async Task<IActionResult> LoadMoreMembers(Guid groupId, int skip = 20, int take = 20)
        {
            const int maxTake = 50;
            if (take > maxTake) take = maxTake;

            var currentUserId = _userManager.GetUserId(User);

            var group = await _context.StudyGroups
                .Where(g => g.Id == groupId)
                .Select(g => new { g.OwnerId })
                .FirstOrDefaultAsync();

            if (group == null) return NotFound();

            var memberData = await _context.StudyGroups
                .Where(g => g.Id == groupId)
                .SelectMany(g => g.Members)
                .Where(m => !m.IsBanned)
                .OrderByDescending(m => m.UserId == group.OwnerId)
                .ThenBy(m => m.User != null ? m.User.UserName : "")
                .Skip(skip)
                .Take(take)
                .Select(m => new
                {
                    m.UserId,
                    UserName = m.User != null ? m.User.UserName : m.UserId,
                    FullName = m.User != null ? m.User.FullName : null,
                    IsOwner = m.UserId == group.OwnerId,
                    RoleName = m.Role.Name,
                    AvatarUrl = m.User != null ? m.User.AvatarUrl : null
                })
                .ToListAsync();

            var onlineUsers = await _presenceTracker.GetOnlineUsers();

            var members = memberData.Select(m => new
            {
                id = m.UserId,
                userName = m.UserName,
                fullName = m.FullName,
                isOwner = m.IsOwner,
                roleName = m.RoleName,
                avatarUrl = m.AvatarUrl,
                isOnline = onlineUsers.Contains(m.UserId),
                connectionStatus = currentUserId == null ? "None" : 
                    _context.Friendships.Any(f => f.RequesterId == currentUserId && f.AddresseeId == m.UserId && f.Status == FriendshipStatus.Accepted) || 
                    _context.Friendships.Any(f => f.RequesterId == m.UserId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Accepted) ? "Accepted" :
                    _context.Friendships.Any(f => f.RequesterId == currentUserId && f.AddresseeId == m.UserId && f.Status == FriendshipStatus.Pending) ? "PendingSent" :
                    _context.Friendships.Any(f => f.RequesterId == m.UserId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Pending) ? "PendingReceived" : "None"
            }).ToList();

            var totalCount = await _context.StudyGroups
                .Where(g => g.Id == groupId)
                .Select(g => g.Members.Count(m => !m.IsBanned))
                .FirstOrDefaultAsync();

            return Json(new
            {
                members,
                hasMore = skip + take < totalCount,
                nextSkip = skip + take
            });
        }

        [HttpPost]
        public async Task<IActionResult> PromoteMember(Guid groupId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            
            // 1. Verify group exists and current user is the owner
            var group = await _context.StudyGroups.FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound();
            if (group.OwnerId != currentUserId) return Forbid();

            // 2. Fetch the "Admin" role for this group
            var adminRole = await _context.GroupRoles.FirstOrDefaultAsync(r => r.GroupId == groupId && r.Name == "Admin");
            if (adminRole == null) return BadRequest("Admin role not found for this group.");

            // 3. Find target membership
            var membership = await _context.GroupMemberships.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
            if (membership == null) return NotFound("Member not found.");

            // 4. Update role
            membership.GroupRoleId = adminRole.Id;
            await _context.SaveChangesAsync(default);

            return Ok();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateStudyGroupCommand command)
        {
            if (!ModelState.IsValid) return View(command);

            command.OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _mediator.Send(command);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var studyGroup = await _mediator.Send(new GetStudyGroupByIdQuery { Id = id });
            if (studyGroup == null) return NotFound();

            // Check if user is owner
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (studyGroup.OwnerId != userId) return Forbid();

            var viewModel = new EditStudyGroupViewModel
            {
                Command = new UpdateStudyGroupCommand
                {
                    Id = studyGroup.Id,
                    Name = studyGroup.Name,
                    Description = studyGroup.Description,
                    SubjectArea = studyGroup.SubjectArea,
                    IsPublic = studyGroup.IsPublic
                },
                Channels = studyGroup.Channels
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditStudyGroupViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var studyGroup = await _mediator.Send(new GetStudyGroupByIdQuery { Id = viewModel.Command.Id });
                viewModel.Channels = studyGroup?.Channels ?? new ();
                return View(viewModel);
            }

            var result = await _mediator.Send(viewModel.Command);
            if (!result) return NotFound();

            return RedirectToAction(nameof(Details), new { id = viewModel.Command.Id });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _mediator.Send(new DeleteStudyGroupCommand { Id = id });
            if (!result) return NotFound();
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CreateChannel(CreateChannelCommand command)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = _userManager.GetUserId(User);
            var isMember = await _context.GroupMemberships.AnyAsync(m => m.GroupId == command.GroupId && m.UserId == userId);
            if (!isMember) return Forbid();

            var channelId = await _mediator.Send(command);
            return RedirectToAction(nameof(Details), new { id = command.GroupId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteChannel(Guid id, Guid groupId)
        {
            var userId = _userManager.GetUserId(User);
            var isMember = await _context.GroupMemberships.AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
            if (!isMember) return Forbid();

            var result = await _mediator.Send(new DeleteChannelCommand { Id = id });
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        [HttpGet]
        public async Task<IActionResult> GetChannelMessages(Guid channelId)
        {
            var messages = await _context.Messages
                .Include(m => m.Author)
                .Where(m => m.ChannelId == channelId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    id = m.Id,
                    content = m.Content,
                    authorName = m.Author != null ? m.Author.FullName ?? m.Author.UserName : "System",
                    authorAvatar = m.Author != null ? m.Author.AvatarUrl ?? $"https://api.dicebear.com/7.x/avataaars/svg?seed={m.Author.UserName}" : "https://api.dicebear.com/7.x/avataaars/svg?seed=System",
                    authorId = m.AuthorId,
                    createdAt = m.CreatedAt.ToString("g"),
                    channelId = m.ChannelId
                })
                .ToListAsync();

            return Json(messages);
        }
    }
}
