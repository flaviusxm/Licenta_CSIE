using Microsoft.AspNetCore.Mvc;
using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.StudyGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AskNLearn.Domain.Entities.Core;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AskNLearn.Web.Controllers;

[Authorize]
[Route("Resources")]
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

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActivePage"] = "Resources";
        var resources = await _context.LearningResources
            .Include(r => r.Uploader)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return View(resources);
    }

    [HttpPost("Delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resource = await _context.LearningResources.FindAsync(id);
        if (resource == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        if (resource.UploaderId != userId && !isAdmin)
        {
            return Forbid();
        }
        
        _context.LearningResources.Remove(resource);
        await _context.SaveChangesAsync(default);
        return Ok();
    }

    [HttpPost("Report")]
    public async Task<IActionResult> Report(Guid id, string reason)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = userId,
            ReportedResourceId = id,
            Reason = ReportReason.Inappropriate, // Defaulting as reason is free text from prompt
            Description = $"User reported with reason: {reason}",
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync(default);

        return Ok(new { message = "Report submitted successfully" });
    }

    [HttpPost("Upload")]
    public async Task<IActionResult> Upload(IFormFile file, string title, string? description)
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
                Description = description,
                Url = fileUrl,
                ResourceType = Path.GetExtension(file.FileName).TrimStart('.').ToUpper(),
                UploaderId = userId,
                GroupId = null, 
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
