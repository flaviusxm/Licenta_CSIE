using System.Net.Http.Json;
using System.Text.Json;
using AskNLearn.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AskNLearn.Infrastructure.Services
{
    public class GuardianClient : IGuardianClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<GuardianClient> _logger;

        public GuardianClient(HttpClient httpClient, IConfiguration config, ILogger<GuardianClient> logger)
        {
            _httpClient = httpClient;
            _baseUrl = config["Guardian:BaseUrl"] ?? "http://localhost:5200"; 
            _logger = logger;
        }

        public async Task<(bool IsSafe, string Reason)> ModerateTextAsync(string content, string? title = null)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/Guardian/moderate", new
                {
                    Content = content,
                    Title = title
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ModerationResult>();
                    return (result?.IsSafe ?? true, result?.Reason ?? "Analiză finalizată.");
                }
                
                return (true, "Guardian service is currently unavailable. Content approved by default.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Guardian microservice for text moderation.");
                return (true, "Connection error with Guardian service.");
            }
        }

        public async Task<(bool IsValid, string Details, string Recommendation)> VerifyDocumentAsync(string? base64Image, string? imageUrl = null)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/Guardian/verify", new
                {
                    ImageBase64 = base64Image,
                    ImageUrl = imageUrl
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<VerificationResult>();
                    return (result?.IsValid ?? false, result?.ExtractionDetails ?? "", result?.Recommendation ?? "No recommendation.");
                }

                return (false, "Guardian service unavailable.", "Contact support.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Guardian microservice for document verification.");
                return (false, "Connection error with Guardian service.", "Please try again later.");
            }
        }

        private class ModerationResult
        {
            public bool IsSafe { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        private class VerificationResult
        {
            public bool IsValid { get; set; }
            public string ExtractionDetails { get; set; } = string.Empty;
            public string Recommendation { get; set; } = string.Empty;
        }
    }
}
