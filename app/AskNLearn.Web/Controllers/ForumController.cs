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
using Microsoft.AspNetCore.Identity;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    [Route("hubs/communities")]
    public class ForumController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ForumController> _logger;
        private readonly UserManager<AskNLearn.Domain.Entities.Core.ApplicationUser> _userManager;
        private readonly AskNLearn.Application.Common.Interfaces.IApplicationDbContext _context;
        private readonly AskNLearn.Application.Common.Interfaces.IModerationQueue _moderationQueue;

        public ForumController(IMediator mediator, ILogger<ForumController> logger, UserManager<AskNLearn.Domain.Entities.Core.ApplicationUser> userManager, AskNLearn.Application.Common.Interfaces.IApplicationDbContext context, AskNLearn.Application.Common.Interfaces.IModerationQueue moderationQueue)
        {
            _mediator = mediator;
            _logger = logger;
            _userManager = userManager;
            _context = context;
            _moderationQueue = moderationQueue;
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var community = await _mediator.Send(new GetCommunityByIdQuery { Id = id, CurrentUserId = userId });
            if (community == null) return NotFound();

            var isEmailVerified = false;
            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                isEmailVerified = user != null && user.VerificationStatus >= AskNLearn.Domain.Entities.Core.UserVerificationStatus.EmailVerified;
            }

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
            ViewBag.IsEmailVerified = isEmailVerified;

            return View(community);
        }

        [AllowAnonymous]
        [HttpGet("v1/discussions/batch")]
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

            var isEmailVerified = false;
            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                isEmailVerified = user != null && user.VerificationStatus >= AskNLearn.Domain.Entities.Core.UserVerificationStatus.EmailVerified;
            }

            ViewBag.CommunityId = communityId;
            ViewBag.CurrentPage = page;
            ViewBag.HasMorePosts = hasMore;
            ViewBag.CurrentUserId = userId;
            ViewBag.CommunityCreatorId = community?.CreatorId;
            ViewBag.IsEmailVerified = isEmailVerified;

            return PartialView("_PostListPartial", posts);
        }

        [AllowAnonymous]
        [HttpGet("v1/comments/retrieve")]
        public async Task<IActionResult> GetPostComments(Guid postId, Guid? communityId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _mediator.Send(new GetPostCommentsQuery
            {
                PostId = postId,
                CommunityId = communityId ?? Guid.Empty,
                CurrentUserId = userId
            });

            ViewData["CurrentUserId"] = userId;
            return PartialView("_PostCommentsPartial", result);
        }

        [HttpPost("v1/interactions/vote")]
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

        [HttpPost("v1/comments/update")]
        public async Task<IActionResult> UpdateComment(Guid id, Guid communityId, string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _mediator.Send(new UpdateCommentCommand { Id = id, UserId = userId, Content = content });
            return RedirectToAction(nameof(Details), new { id = communityId });
        }

        [HttpPost("v1/comments/delete")]
        public async Task<IActionResult> DeleteComment(Guid id, Guid communityId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _mediator.Send(new DeleteCommentCommand { Id = id, UserId = userId });
            return RedirectToAction(nameof(Details), new { id = communityId });
        }


        [HttpPost("v1/discussions/comments/add")]
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

                var user = await _userManager.FindByIdAsync(userId);
                var isEmailVerified = user != null && user.VerificationStatus >= AskNLearn.Domain.Entities.Core.UserVerificationStatus.EmailVerified;
                if (!isEmailVerified)
                {
                    _logger.LogWarning("Action blocked: User {UserId} attempted to comment or post without verification.", userId);
                    return RedirectToAction(nameof(Details), new { id = command.CommunityId });
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
                    ViewData["CurrentUserId"] = userId;
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

        [HttpGet("initiate")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("initiate")]
        public async Task<IActionResult> Create(CreateCommunityCommand command)
        {
            if (!ModelState.IsValid) return View(command);

            command.CreatorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _mediator.Send(command);

            return RedirectToAction("Index", "Explore");
        }

        [HttpGet("manage/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var community = await _mediator.Send(new GetCommunityByIdQuery { Id = id });
            if (community == null) return NotFound();

            // Check if user is creator or admin
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (community.CreatorId != userId && !isAdmin) return Forbid();

            var command = new UpdateCommunityCommand
            {
                Id = community.Id,
                Name = community.Name,
                Description = community.Description
            };

            return View(command);
        }

        [HttpPost("manage")]
        public async Task<IActionResult> Edit(UpdateCommunityCommand command)
        {
            if (!ModelState.IsValid) return View(command);

            var community = await _mediator.Send(new GetCommunityByIdQuery { Id = command.Id });
            if (community == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (community.CreatorId != userId && !isAdmin) return Forbid();

            var result = await _mediator.Send(command);
            if (!result) return NotFound();

            return RedirectToAction("Index", "Explore");
        }

        [HttpPost("terminate/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var community = await _mediator.Send(new GetCommunityByIdQuery { Id = id });
            if (community == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (community.CreatorId != userId && !isAdmin) return Forbid();

            var result = await _mediator.Send(new DeleteCommunityCommand { Id = id });
            if (!result) return NotFound();

            return RedirectToAction("Index", "Explore");
        }

        [HttpGet("hub/{communityId:guid}/discussions/initiate")]
        public IActionResult CreatePost(Guid communityId)
        {
            var command = new CreatePostCommand { CommunityId = communityId };
            return View(command);
        }

        [HttpPost("hub/discussions/initiate")]
        public async Task<IActionResult> CreatePost(CreatePostCommand command)
        {
            if (!ModelState.IsValid) return View(command);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var isEmailVerified = user != null && user.VerificationStatus >= AskNLearn.Domain.Entities.Core.UserVerificationStatus.EmailVerified;
            
            if (!isEmailVerified)
            {
                return RedirectToAction(nameof(Details), new { id = command.CommunityId });
            }

            command.AuthorId = userId;
            await _mediator.Send(command);

            return RedirectToAction(nameof(Details), new { id = command.CommunityId });
        }

        [HttpGet("discussions/manage/{id:guid}")]
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

        [HttpPost("discussions/manage")]
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

        [HttpPost("discussions/terminate/{id:guid}")]
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

        [HttpPost("discussions/v1/solve-toggle/{id:guid}")]
        public async Task<IActionResult> ToggleSolved(Guid id, Guid communityId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _mediator.Send(new TogglePostSolvedCommand { Id = id, UserId = userId });
            return RedirectToAction(nameof(Details), new { id = communityId });
        }
        [AllowAnonymous]
        [HttpGet("hover-card/{id:guid}")]
        public async Task<IActionResult> GetHoverCard(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var community = await _mediator.Send(new GetCommunityByIdQuery { Id = id, CurrentUserId = userId });
            if (community == null) return NotFound();
            return PartialView("_CommunityHoverCard", community);
        }
        [HttpPost("v1/discussions/report")]
        public async Task<IActionResult> ReportPost(Guid id, AskNLearn.Domain.Entities.Core.ReportReason reason, string description)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var report = new AskNLearn.Domain.Entities.Core.Report
            {
                ReporterId = userId,
                ReportedPostId = id,
                Reason = reason,
                Description = description ?? "No description provided",
                CreatedAt = DateTime.UtcNow,
                Status = AskNLearn.Domain.Entities.Core.ReportStatus.Pending
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync(default);

            // Enqueue for AI Re-evaluation
            _moderationQueue.Enqueue(new AskNLearn.Application.Common.Interfaces.ModerationTask
            {
                Id = report.Id, // We use ReportId so Background service knows it's a manual report
                Content = post.Content,
                Title = post.Title,
                Target = AskNLearn.Application.Common.Interfaces.ModerationTarget.Report,
                Reason = reason
            });

            return Ok(new { message = "Post reported successfully. Guardian AI is analyzing." });
        }

        [HttpPost("v1/comments/report")]
        public async Task<IActionResult> ReportComment(Guid id, AskNLearn.Domain.Entities.Core.ReportReason reason, string description)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();

            var report = new AskNLearn.Domain.Entities.Core.Report
            {
                ReporterId = userId,
                ReportedMessageId = id,
                Reason = reason,
                Description = description ?? "No description provided",
                CreatedAt = DateTime.UtcNow,
                Status = AskNLearn.Domain.Entities.Core.ReportStatus.Pending
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync(default);

            // Enqueue for AI Re-evaluation
            _moderationQueue.Enqueue(new AskNLearn.Application.Common.Interfaces.ModerationTask
            {
                Id = report.Id,
                Content = message.Content ?? string.Empty,
                Target = AskNLearn.Application.Common.Interfaces.ModerationTarget.Report,
                Reason = reason
            });

            return Ok(new { message = "Comment reported successfully. Guardian AI is analyzing." });
        }
    }
}
