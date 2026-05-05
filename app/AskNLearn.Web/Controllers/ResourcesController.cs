using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Core;
using AskNLearn.Infrastructure.Persistance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskNLearn.Web.Controllers
{
    [Authorize]
    [Route("resources")]
    public class ResourcesController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager, 
        IWebHostEnvironment environment,
        IReputationService reputationService) : Controller
    {
        [HttpGet("")]
        public async Task<IActionResult> Index(string? searchTerm, string? type, int skip = 0, int take = 15)
        {
            var query = context.StoredFiles
                .Include(f => f.Uploader)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(f => f.FileName.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(f => f.FileType != null && f.FileType.Contains(type));
            }

            var files = await query
                .OrderByDescending(f => f.UploadedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ResourceList", files);
            }

            return View(files);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            var userId = userManager.GetUserId(User);
            var user = await userManager.FindByIdAsync(userId);
            if (user == null || user.VerificationStatus != UserVerificationStatus.IdentityVerified)
            {
                return RedirectToAction(nameof(Index));
            }

            var uploadsPath = Path.Combine(environment.WebRootPath, "uploads", "resources");
            if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

            var fileId = Guid.NewGuid();
            var extension = Path.GetExtension(file.FileName);
            var filePath = Path.Combine("uploads", "resources", $"{fileId}{extension}");
            var absolutePath = Path.Combine(environment.WebRootPath, filePath);

            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var storedFile = new StoredFile
            {
                Id = fileId,
                FileName = file.FileName,
                FilePath = "/" + filePath.Replace("\\", "/"),
                FileType = file.ContentType,
                FileSize = file.Length,
                UploaderId = userId,
                UploadedAt = DateTime.UtcNow,
                ModuleContext = "Resources"
            };

            context.StoredFiles.Add(storedFile);
            await context.SaveChangesAsync();

            // Add Reputation Points (+15 for new resource)
            await reputationService.AddPointsAsync(userId, 15);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = userManager.GetUserId(User);
            var file = await context.StoredFiles.FindAsync(id);

            if (file == null) return NotFound();
            if (file.UploaderId != userId && !User.IsInRole("Admin")) return Forbid();

            var absolutePath = Path.Combine(environment.WebRootPath, file.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(absolutePath)) System.IO.File.Delete(absolutePath);

            context.StoredFiles.Remove(file);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> Download(Guid id)
        {
            var file = await context.StoredFiles.FindAsync(id);
            if (file == null) return NotFound();

            var absolutePath = Path.Combine(environment.WebRootPath, file.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(absolutePath)) return NotFound();

            var bytes = await System.IO.File.ReadAllBytesAsync(absolutePath);
            return File(bytes, file.FileType ?? "application/octet-stream", file.FileName);
        }
    }
}
