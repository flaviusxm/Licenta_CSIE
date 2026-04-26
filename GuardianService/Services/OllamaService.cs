using System.Net.Http.Json;
using System.Text.Json;
using GuardianService.DTOs;
using GuardianService.Models;

namespace GuardianService.Services
{
    public interface IOllamaService
    {
        Task<ModerationResponse> AnalyzeTextAsync(string content, string? title = null);
        Task<VerificationResponse> AnalyzeImageAsync(string? base64Image, string? imageUrl = null);
    }

    public class OllamaService : IOllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<OllamaService> _logger;
        private readonly IPromptService _promptService;

        public OllamaService(HttpClient httpClient, IConfiguration config, ILogger<OllamaService> logger, IPromptService promptService)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _promptService = promptService;
        }

        public async Task<ModerationResponse> AnalyzeTextAsync(string content, string? title = null)
        {
            var model = _config["Ollama:TextModel"] ?? "llama3.1";
            var baseUrl = _config["Ollama:BaseUrl"] ?? "http://localhost:11434";

            var prompt = _promptService.GetModerationPrompt(content, title);

            try
            {
                var request = new OllamaGenerateRequest
                {
                    Model = model,
                    Prompt = prompt,
                    Format = "json",
                    Stream = false
                };

                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/generate", request);

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                var jsonText = result.GetProperty("response").GetString();

                var moderationResult = JsonSerializer.Deserialize<ModerationResponse>(jsonText!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return moderationResult ?? new ModerationResponse { IsSafe = true, Reason = "Analiză eșuată, aprobat implicit." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Ollama for text moderation.");
                return new ModerationResponse { IsSafe = true, Reason = "Eroare tehnică la procesare AI." };
            }
        }

        public async Task<VerificationResponse> AnalyzeImageAsync(string? base64Image, string? imageUrl = null)
        {
            var model = _config["Ollama:VisionModel"] ?? "llava";
            var baseUrl = _config["Ollama:BaseUrl"] ?? "http://localhost:11434";

            var prompt = _promptService.GetVerificationPrompt();

            try
            {
                var request = new OllamaGenerateRequest
                {
                    Model = model,
                    Prompt = prompt,
                    Format = "json",
                    Stream = false,
                    Images = base64Image != null ? new[] { base64Image } : null
                };

                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/generate", request);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                var jsonText = result.GetProperty("response").GetString();

                var verificationResult = JsonSerializer.Deserialize<VerificationResponse>(jsonText!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return verificationResult ?? new VerificationResponse { IsValid = false, Recommendation = "Nu s-a putut analiza imaginea." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Ollama for image analysis.");
                return new VerificationResponse { IsValid = false, Recommendation = "Eroare tehnică la analiză imagine." };
            }
        }
    }
}
