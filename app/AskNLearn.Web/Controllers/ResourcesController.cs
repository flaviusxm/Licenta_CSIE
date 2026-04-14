using Microsoft.AspNetCore.Mvc;
using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.StudyGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AskNLearn.Domain.Entities.Core;
using System.Security.Claims;

namespace AskNLearn.Web.Controllers;

public class ResourcesController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ResourcesController(
        IApplicationDbContext context, 
        IFileService fileService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _fileService = fileService;
        _userManager = userManager;
    }
        public async Task<IActionResult> Index()
        {
            ViewData["ActivePage"] = "Resources";
            var resources = await _context.LearningResources
                .Include(r => r.Uploader)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(resources);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var resource = await _context.LearningResources.FindAsync(id);
            if (resource == null) return NotFound();
            
            _context.LearningResources.Remove(resource);
            await _context.SaveChangesAsync(default);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Report(Guid id, string reason, string description)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var report = new Report
            {
                Id = Guid.NewGuid(),
                ReporterId = userId,
                ReportedResourceId = id,
                Reason = Enum.Parse<ReportReason>(reason),
                Description = description ?? "No description provided",
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync(default);

            return Ok(new { message = "Report submitted successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, string title)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");
            if (string.IsNullOrEmpty(title)) return BadRequest("Title is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                using var stream = file.OpenReadStream();
                var fileUrl = await _fileService.UploadFileAsync(stream, file.FileName, "resources");

                var resource = new LearningResource
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Url = fileUrl,
                    ResourceType = Path.GetExtension(file.FileName).TrimStart('.').ToUpper(),
                    UploaderId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.LearningResources.Add(resource);
                await _context.SaveChangesAsync(default);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return BadRequest($"Error uploading file: {ex.Message}");
            }
        }
}
