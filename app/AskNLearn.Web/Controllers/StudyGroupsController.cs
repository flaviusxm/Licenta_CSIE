using AskNLearn.Application.Features.StudyGroups.Commands.CreateChannel;
using AskNLearn.Application.Features.StudyGroups.Commands.CreateStudyGroup;
using AskNLearn.Application.Features.StudyGroups.Commands.DeleteStudyGroup;
using AskNLearn.Application.Features.StudyGroups.Commands.UpdateStudyGroup;
using AskNLearn.Application.Features.StudyGroups.Queries;
using AskNLearn.Application.Features.StudyGroups.Queries.GetStudyGroupById;
using AskNLearn.Application.Features.StudyGroups.Queries.GetStudyGroups;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    public class StudyGroupsController : Controller
    {
        private readonly IMediator _mediator;

        public StudyGroupsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchTerm)
        {
            var groups = await _mediator.Send(new GetStudyGroupsQuery { SearchTerm = searchTerm });
            return View(groups);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid id)
        {
            var studyGroup = await _mediator.Send(new GetStudyGroupByIdQuery { Id = id });
            if (studyGroup == null) return NotFound();

            return View(studyGroup);
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

            var command = new UpdateStudyGroupCommand
            {
                Id = studyGroup.Id,
                Name = studyGroup.Name,
                Description = studyGroup.Description,
                SubjectArea = studyGroup.SubjectArea,
                IsPublic = studyGroup.IsPublic
            };

            return View(command);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UpdateStudyGroupCommand command)
        {
            if (!ModelState.IsValid) return View(command);

            var result = await _mediator.Send(command);
            if (!result) return NotFound();

            return RedirectToAction(nameof(Index));
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
    }
}
