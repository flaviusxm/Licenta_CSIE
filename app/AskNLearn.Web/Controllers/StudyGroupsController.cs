using AskNLearn.Domain.Entities.Core;
using AskNLearn.Application.Common.Interfaces;
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
    public class StudyGroupsController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudyGroupsController(IMediator mediator, IApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _mediator = mediator;
            _context = context;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchTerm)
        {
            var groups = await _mediator.Send(new GetStudyGroupsQuery 
            { 
                SearchTerm = searchTerm,
                Skip = 0,
                Take = 8
            });
            return View(groups);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetGroups(int skip, string? searchTerm)
        {
            var groups = await _mediator.Send(new GetStudyGroupsQuery 
            { 
                SearchTerm = searchTerm,
                Skip = skip,
                Take = 8
            });
            return PartialView("_GroupCards", groups);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUserId = _userManager.GetUserId(User);
            var studyGroup = await _mediator.Send(new AskNLearn.Application.Features.StudyGroups.Queries.GetStudyGroupById.GetStudyGroupByIdQuery { Id = id, CurrentUserId = currentUserId });
            if (studyGroup == null) return NotFound();

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

            var members = await _context.StudyGroups
                .Where(g => g.Id == groupId)
                .SelectMany(g => g.Members)
                .Where(m => !m.IsBanned)
                .OrderByDescending(m => m.UserId == group.OwnerId)
                .ThenBy(m => m.User != null ? m.User.UserName : "")
                .Skip(skip)
                .Take(take)
                .Select(m => new
                {
                    id = m.UserId,
                    userName = m.User != null ? m.User.UserName : m.UserId,
                    fullName = m.User != null ? m.User.FullName : null,
                    isOwner = m.UserId == group.OwnerId,
                    connectionStatus = currentUserId == null ? "None" : 
                        _context.Friendships.Any(f => f.RequesterId == currentUserId && f.AddresseeId == m.UserId && f.Status == FriendshipStatus.Accepted) || 
                        _context.Friendships.Any(f => f.RequesterId == m.UserId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Accepted) ? "Accepted" :
                        _context.Friendships.Any(f => f.RequesterId == currentUserId && f.AddresseeId == m.UserId && f.Status == FriendshipStatus.Pending) ? "PendingSent" :
                        _context.Friendships.Any(f => f.RequesterId == m.UserId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Pending) ? "PendingReceived" : "None"
                })
                .ToListAsync();

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

            var channelId = await _mediator.Send(command);
            return RedirectToAction(nameof(Details), new { id = command.GroupId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteChannel(Guid id, Guid groupId)
        {
            var result = await _mediator.Send(new DeleteChannelCommand { Id = id });
            return RedirectToAction(nameof(Edit), new { id = groupId });
        }
    }
}
