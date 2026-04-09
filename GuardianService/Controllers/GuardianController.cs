using Microsoft.AspNetCore.Mvc;
using GuardianService.DTOs;
using GuardianService.Services;

namespace GuardianService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GuardianController : ControllerBase
    {
        private readonly IOllamaService _ollamaService;

        public GuardianController(IOllamaService ollamaService)
        {
            _ollamaService = ollamaService;
        }

        [HttpPost("moderate")]
        public async Task<IActionResult> Moderate([FromBody] ModerationRequest request)
        {
            var result = await _ollamaService.AnalyzeTextAsync(request.Content, request.Title);
            return Ok(result);
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] VerificationRequest request)
        {
            var result = await _ollamaService.AnalyzeImageAsync(request.ImageBase64, request.ImageUrl);
            return Ok(result);
        }
    }
}
