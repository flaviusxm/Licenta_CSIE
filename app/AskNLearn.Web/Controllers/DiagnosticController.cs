using AskNLearn.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace AskNLearn.Web.Controllers
{
    [Route("adminRoute/[controller]/[action]")]
    public class DiagnosticController : Controller
    {
        private readonly IApplicationDbContext _context;

        public DiagnosticController(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> DataReport()
        {
            var report = new
            {
                Users = await _context.Users.CountAsync(),
                Communities = await _context.Communities.CountAsync(),
                Posts = await _context.Posts.CountAsync(),
                Messages = await _context.Messages.CountAsync(),
                VerificationRequests = await _context.VerificationRequests.CountAsync(),
                StudyGroups = await _context.StudyGroups.CountAsync(),
                Channels = await _context.Channels.CountAsync(),
                Events = await _context.Events.CountAsync(),
                Votes = await _context.PostVotes.CountAsync(),
                Ranks = await _context.UserRanks.CountAsync(),
                DatabaseProvider = _context is DbContext dc ? dc.Database.ProviderName : "Unknown",
                ConnectionString = _context is DbContext db ? db.Database.GetDbConnection().ConnectionString.Split(';').Select(s => s.Contains("Password") ? "Password=***" : s).Aggregate((a, b) => a + ";" + b) : "Hidden"
            };

            return Json(report);
        }
    }
}
