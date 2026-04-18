using AskNLearn.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace AskNLearn.Infrastructure.Services
{
    public class OllamaService : IOllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaService> _logger;
        private const string OllamaUrl = "http://localhost:11434/api/generate";

        public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<(bool IsSafe, string Reason)> AnalyzeTextAsync(string content, string prompt, string model = "qwen2.5:0.5b")
        {
            try
            {
                var requestBody = new
                {
                    model = model,
                    prompt = $"{prompt}\n\nContent: {content}",
                    stream = false,
                    format = "json"
                };

                var response = await _httpClient.PostAsJsonAsync(OllamaUrl, requestBody);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var responseText = jsonResponse.GetProperty("response").GetString();

                if (string.IsNullOrEmpty(responseText))
                    return (true, "No analysis response.");

                var result = JsonSerializer.Deserialize<ModerationResult>(responseText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return (result?.IsSafe ?? true, result?.Reason ?? "Analysis completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Ollama for text moderation.");
                return (true, "AI processing error.");
            }
        }

        public async Task<(bool IsValid, string Details, string Recommendation)> AnalyzeImageAsync(byte[] imageBytes, string prompt, string model = "moondream")
        {
            try
            {
                var base64Image = Convert.ToBase64String(imageBytes);
                var requestBody = new
                {
                    model = model,
                    prompt = prompt,
                    images = new[] { base64Image },
                    stream = false
                };

                var response = await _httpClient.PostAsJsonAsync(OllamaUrl, requestBody);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var responseText = jsonResponse.GetProperty("response").GetString();

                if (string.IsNullOrEmpty(responseText))
                    return (false, "Empty vision response.", "Review Required");

                // moondream usually returns text, but we can try to extract patterns
                bool isValid = responseText.Contains("valid", StringComparison.OrdinalIgnoreCase) || 
                              responseText.Contains("student", StringComparison.OrdinalIgnoreCase) ||
                              responseText.Contains("id card", StringComparison.OrdinalIgnoreCase);

                return (isValid, responseText, isValid ? "Approved" : "Needs Manual Review");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Ollama for vision analysis.");
                return (false, "Vision API error.", "Review Required");
            }
        }

        private class ModerationResult
        {
            public bool IsSafe { get; set; }
            public string Reason { get; set; } = string.Empty;
        }
    }
}
