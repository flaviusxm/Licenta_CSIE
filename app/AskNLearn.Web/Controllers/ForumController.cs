using AskNLearn.Application.Features.Communities.Commands.CreateCommunity;
using AskNLearn.Application.Features.Communities.Commands.DeleteCommunity;
using AskNLearn.Application.Features.Communities.Commands.UpdateCommunity;
using AskNLearn.Application.Features.Communities.Queries.GetCommunities;
using AskNLearn.Application.Features.Communities.Queries.GetCommunityById;
using AskNLearn.Application.Features.Posts.Commands.CreatePost;
using AskNLearn.Application.Features.Posts.Commands.DeletePost;
using AskNLearn.Application.Features.Posts.Commands.UpdatePost;
using AskNLearn.Application.Features.Posts.Queries.GetPostById;
using AskNLearn.Application.Features.Posts.Queries.GetPostsByCommunity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

using AskNLearn.Application.Features.Posts.Commands.AddComment;
using AskNLearn.Application.Features.Posts.Commands.RecordPostView;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    public class ForumController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ForumController> _logger;

        public ForumController(IMediator mediator, ILogger<ForumController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid id)
        {
            var community = await _mediator.Send(new GetCommunityByIdQuery { Id = id });
            if (community == null) return NotFound();

            var posts = await _mediator.Send(new GetPostsByCommunityQuery { CommunityId = id });
            ViewBag.Posts = posts;

            return View(community);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment([FromForm] AddCommentCommand command)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    _logger.LogWarning("Invalid model state for AddComment: {Errors}. PostId: {PostId}", errors, command?.PostId);
                    return RedirectToAction(nameof(Details), new { id = command?.CommunityId });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                if (command.Attachment != null)
                {
                    _logger.LogInformation("AddComment received attachment: {FileName}, Size: {Size}, Type: {Type}", 
                        command.Attachment.FileName, command.Attachment.Length, command.Attachment.ContentType);
                }

                command.AuthorId = userId;
                await _mediator.Send(command);

                return RedirectToAction(nameof(Details), new { id = command.CommunityId });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "FATAL ERROR in ForumController.AddComment for Post {PostId}", command?.PostId);
                throw;
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCommunityCommand command)
        {
            if (!ModelState.IsValid) return View(command);

            command.CreatorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _mediator.Send(command);

            return RedirectToAction("Index", "Explore");
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var community = await _mediator.Send(new GetCommunityByIdQuery { Id = id });
            if (community == null) return NotFound();

            var command = new UpdateCommunityCommand
            {
                Id = community.Id,
                Name = community.Name,
                Description = community.Description,
                IsPublic = community.IsPublic
            };

            return View(command);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UpdateCommunityCommand command)
        {
            if (!ModelState.IsValid) return View(command);

            var result = await _mediator.Send(command);
            if (!result) return NotFound();

            return RedirectToAction("Index", "Explore");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _mediator.Send(new DeleteCommunityCommand { Id = id });
            if (!result) return NotFound();

            return RedirectToAction("Index", "Explore");
        }

        public IActionResult CreatePost(Guid communityId)
        {
            var command = new CreatePostCommand { CommunityId = communityId };
            return View(command);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(CreatePostCommand command)
        {
            if (!ModelState.IsValid) return View(command);

            command.AuthorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _mediator.Send(command);

            return RedirectToAction(nameof(Details), new { id = command.CommunityId });
        }

        public async Task<IActionResult> EditPost(Guid id)
        {
            var post = await _mediator.Send(new GetPostByIdQuery { Id = id });
            if (post == null) return NotFound();

            var command = new UpdatePostCommand
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                IsSolved = post.IsSolved
            };

            return View(command);
        }

        [HttpPost]
        public async Task<IActionResult> EditPost(UpdatePostCommand command, Guid communityId)
        {
            if (!ModelState.IsValid) return View(command);

            var result = await _mediator.Send(command);
            if (!result) return NotFound();

            return RedirectToAction(nameof(Details), new { id = communityId });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost(Guid id, Guid communityId)
        {
            var result = await _mediator.Send(new DeletePostCommand { Id = id });
            if (!result) return NotFound();

            return RedirectToAction(nameof(Details), new { id = communityId });
        }
    }
}
