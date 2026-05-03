using AskNLearn.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

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
            _httpClient.Timeout = TimeSpan.FromMinutes(3); // Vision needs more time
        }

        private async Task<byte[]> PreprocessImageAsync(byte[] imageBytes)
        {
            try
            {
                using var image = Image.Load(imageBytes);
                
                // 1. Resize if too large (Optimization for Moondream)
                int maxWidth = 1200;
                if (image.Width > maxWidth)
                {
                    int newHeight = (int)((double)image.Height / image.Width * maxWidth);
                    image.Mutate(x => x.Resize(maxWidth, newHeight));
                }

                // 2. Enhance for OCR: Auto Contrast + Sharpen
                image.Mutate(x => x
                    .Contrast(1.1f)
                    .GaussianSharpen(0.4f));

                using var ms = new MemoryStream();
                await image.SaveAsync(ms, new JpegEncoder { Quality = 85 });
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Ollama] Could not preprocess image, sending raw bytes.");
                return imageBytes;
            }
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
            int maxRetries = 2;
            int attempt = 0;

            // Pre-procesăm imaginea o singură dată
            var processedBytes = await PreprocessImageAsync(imageBytes);

            while (attempt < maxRetries)
            {
                attempt++;
                try
                {
                    _logger.LogInformation("[Ollama] Starting vision analysis with model {Model} (Attempt {Attempt}). Processed Size: {Size} bytes", model, attempt, processedBytes.Length);
                    var base64Image = Convert.ToBase64String(processedBytes);
                    var requestBody = new
                    {
                        model = model,
                        prompt = prompt,
                        images = new[] { base64Image },
                        stream = false
                    };

                    var startTime = DateTime.UtcNow;
                    var response = await _httpClient.PostAsJsonAsync(OllamaUrl, requestBody);
                    var duration = DateTime.UtcNow - startTime;

                    _logger.LogInformation("[Ollama] API responded in {Duration}ms with status {Status}", duration.TotalMilliseconds, response.StatusCode);
                    
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var responseText = jsonResponse.GetProperty("response").GetString();

                    if (string.IsNullOrEmpty(responseText))
                    {
                        if (attempt < maxRetries)
                        {
                            _logger.LogWarning("[Ollama] Received empty response. Retrying...");
                            await Task.Delay(1000);
                            continue;
                        }
                        return (false, "Empty vision response after retries.", "Review Required");
                    }

                    _logger.LogDebug("[Ollama] Raw AI Response: {Response}", responseText);

                    // Logică mai permisivă pentru aprobare
                    bool isValid = responseText.Contains("valid", StringComparison.OrdinalIgnoreCase) || 
                                   responseText.Contains("approve", StringComparison.OrdinalIgnoreCase) ||
                                   responseText.Contains("yes", StringComparison.OrdinalIgnoreCase) ||
                                   responseText.Contains("student", StringComparison.OrdinalIgnoreCase);

                    return (isValid, responseText, isValid ? "Approved" : "Needs Manual Review");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Ollama] Critical error during vision analysis (Attempt {Attempt}).", attempt);
                    if (attempt >= maxRetries) return (false, "Vision API error: " + ex.Message, "Review Required");
                    await Task.Delay(2000);
                }
            }

            return (false, "AI failed to respond.", "Needs Manual Review");
        }

        private class ModerationResult
        {
            public bool IsSafe { get; set; }
            public string Reason { get; set; } = string.Empty;
        }
    }
}
