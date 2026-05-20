using AskNLearn.Application.Common.Interfaces;
using AskNLearn.Domain.Entities.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AskNLearn.Web.Controllers.Api
{
    [Route("api/trustlayer")]
    [ApiController]
    public class TrustLayerController : ControllerBase
    {
        private readonly IApplicationDbContext _context;

        public TrustLayerController(IApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("logs/{category}")]
        public async Task<IActionResult> GetLogs(string category, int skip = 0, int take = 20)
        {

            if (category == "id")
            {
                var logs = await _context.VerificationRequests
                    .Include(v => v.User)
                    .OrderByDescending(v => v.SubmittedAt)
                    .Select(v => new
                    {
                        Timestamp = v.SubmittedAt,
                        Source = v.User.FullName,
                        Action = "Identity OCR Scan",
                        Details = v.AdminNotes ?? "Biometric evaluation in progress",
                        Status = v.Status.ToString(),
                        Model = "Llama 3.2 Vision"
                    })
                    .Skip(skip).Take(take)
                    .ToListAsync();
                return Ok(logs);
            }
            
            if (category == "resources")
            {
                var logs = await _context.Posts
                    .Include(p => p.Author)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new
                    {
                        Timestamp = p.CreatedAt,
                        Source = p.Author.UserName,
                        Action = "Content Shield",
                        Details = p.ModerationReason ?? "Analyzing semantic safety",
                        Status = p.ModerationStatus.ToString(),
                        Model = "Qwen-Moderator"
                    })
                    .Skip(skip).Take(take)
                    .ToListAsync();
                return Ok(logs);
            }

            if (category == "communities")
            {
                var logs = await _context.Reports
                    .Include(r => r.Reporter)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        Timestamp = r.CreatedAt,
                        Source = r.Reporter.UserName,
                        Action = "Community Pulse",
                        Details = r.Reason.ToString() + ": " + r.Description,
                        Status = r.Status.ToString(),
                        Model = "TrustLayer-Core"
                    })
                    .Skip(skip).Take(take)
                    .ToListAsync();
                return Ok(logs);
            }

            return BadRequest("Invalid category");
        }
    }
}
