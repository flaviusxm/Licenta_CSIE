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
using AskNLearn.Application.Features.Posts.Queries.GetPostComments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using AskNLearn.Application.Features.Posts.Commands.AddComment;
using AskNLearn.Application.Features.Posts.Commands.RecordPostView;
using AskNLearn.Application.Features.Communities.Commands.JoinCommunity;
using AskNLearn.Application.Features.Communities.Commands.LeaveCommunity;
using AskNLearn.Application.Features.Posts.Commands.VotePost;
using AskNLearn.Application.Features.Posts.Commands.DeleteComment;
using AskNLearn.Application.Features.Posts.Commands.UpdateComment;
using AskNLearn.Application.Features.Posts.Commands.TogglePostSolved;

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var community = await _mediator.Send(new GetCommunityByIdQuery { Id = id, CurrentUserId = userId });
            if (community == null) return NotFound();

            const int pageSize = 10;
            var posts = await _mediator.Send(new GetPostsByCommunityQuery
            {
                CommunityId = id,
                CurrentUserId = userId,
                Page = 1,
                PageSize = pageSize
            });
            var totalCount = await _mediator.Send(new GetPostsCountByCommunityQuery { CommunityId = id });

            ViewBag.Posts = posts;
            ViewBag.TotalPostCount = totalCount;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = 1;

            return View(community);
        }

        [AllowAnonymous]
        public async Task<IActionResult> LoadMorePosts(Guid communityId, int page = 2)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            const int pageSize = 10;

            var posts = await _mediator.Send(new GetPostsByCommunityQuery
            {
                CommunityId = communityId,
                CurrentUserId = userId,
                Page = page,
                PageSize = pageSize
            });

            var totalCount = await _mediator.Send(new GetPostsCountByCommunityQuery { CommunityId = communityId });
            var hasMore = page * pageSize < totalCount;

            var community = await _mediator.Send(new GetCommunityByIdQuery { Id = communityId, CurrentUserId = userId });

            ViewBag.CommunityId = communityId;
            ViewBag.CurrentPage = page;
            ViewBag.HasMorePosts = hasMore;
            ViewBag.CurrentUserId = userId;
            ViewBag.CommunityCreatorId = community?.CreatorId;

            return PartialView("_PostListPartial", posts);
        }

        [AllowAnonymous]
        public async Task<IActionResult> GetPostComments(Guid postId, Guid communityId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _mediator.Send(new GetPostCommentsQuery
            {
                PostId = postId,
                CommunityId = communityId,
                CurrentUserId = userId
            });

            return PartialView("_PostCommentsPartial", result);
        }

        [HttpPost]
        public async Task<IActionResult> Join(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _mediator.Send(new JoinCommunityCommand { CommunityId = id, UserId = userId });
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Leave(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _mediator.Send(new LeaveCommunityCommand { CommunityId = id, UserId = userId });
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> VotePost(Guid postId, Guid communityId, short value)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _mediator.Send(new VotePostCommand { PostId = postId, UserId = userId, Value = value });
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                (Request.Headers["Accept"].ToString().Contains("application/json")))
            {
                return Json(new { 
                    success = result.Success, 
                    voteCount = result.VoteCount,
                    userVote = result.UserVote
                });
            }

            return RedirectToAction(nameof(Details), new { id = communityId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateComment(Guid id, Guid communityId, string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _mediator.Send(new UpdateCommentCommand { Id = id, UserId = userId, Content = content });
            return RedirectToAction(nameof(Details), new { id = communityId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteComment(Guid id, Guid communityId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _mediator.Send(new DeleteCommentCommand { Id = id, UserId = userId });
            return RedirectToAction(nameof(Details), new { id = communityId });
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
                var resultId = await _mediator.Send(command);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                    Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    var comments = await _mediator.Send(new GetPostCommentsQuery 
                    { 
                        PostId = command.PostId, 
                        CommunityId = command.CommunityId ?? Guid.Empty, 
                        CurrentUserId = userId 
                    });
                    return PartialView("_PostCommentsPartial", comments);
                }

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

            // Check if user is creator
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (community.CreatorId != userId) return Forbid();

            var command = new UpdateCommunityCommand
            {
                Id = community.Id,
                Name = community.Name,
                Description = community.Description
            };

            return View(command);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UpdateCommunityCommand command)
        {
            if (!ModelState.IsValid) return View(command);

            var community = await _mediator.Send(new GetCommunityByIdQuery { Id = command.Id });
            if (community == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (community.CreatorId != userId) return Forbid();

            var result = await _mediator.Send(command);
            if (!result) return NotFound();

            return RedirectToAction("Index", "Explore");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var community = await _mediator.Send(new GetCommunityByIdQuery { Id = id });
            if (community == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (community.CreatorId != userId) return Forbid();

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

            // Check if user is author
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (post.AuthorId != userId) return Forbid();

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

            var post = await _mediator.Send(new GetPostByIdQuery { Id = command.Id });
            if (post == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (post.AuthorId != userId) return Forbid();

            var result = await _mediator.Send(command);
            if (!result) return NotFound();

            return RedirectToAction(nameof(Details), new { id = communityId });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost(Guid id, Guid communityId)
        {
            var post = await _mediator.Send(new GetPostByIdQuery { Id = id });
            if (post == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (post.AuthorId != userId) return Forbid();

            var result = await _mediator.Send(new DeletePostCommand { Id = id });
            if (!result) return NotFound();

            return RedirectToAction(nameof(Details), new { id = communityId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSolved(Guid id, Guid communityId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _mediator.Send(new TogglePostSolvedCommand { Id = id, UserId = userId });
            return RedirectToAction(nameof(Details), new { id = communityId });
        }
        [AllowAnonymous]
        public async Task<IActionResult> GetHoverCard(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var community = await _mediator.Send(new GetCommunityByIdQuery { Id = id, CurrentUserId = userId });
            if (community == null) return NotFound();
            return PartialView("_CommunityHoverCard", community);
        }
    }
}
