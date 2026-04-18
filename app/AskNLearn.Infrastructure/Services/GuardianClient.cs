using System.Net.Http.Json;
using System.Text.Json;
using AskNLearn.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AskNLearn.Infrastructure.Services
{
    public class GuardianClient : IGuardianClient
    {
        private readonly IOllamaService _ollama;
        private readonly ILogger<GuardianClient> _logger;

        public GuardianClient(IOllamaService ollama, ILogger<GuardianClient> logger)
        {
            _ollama = ollama;
            _logger = logger;
        }

        public async Task<(bool IsSafe, string Reason)> ModerateTextAsync(string content, string? title = null)
        {
            var prompt = @"Analizează următorul conținut academic. Căutăm: limbaj licențios, hărțuire, spam, amenințări de securitate sau cod malițios. 
            Răspunde DOAR în format JSON: { ""isSafe"": boolean, ""reason"": ""scurtă explicație"" }";
            
            return await _ollama.AnalyzeTextAsync(content, prompt, "qwen2.5:0.5b");
        }

        public async Task<(bool IsValid, string Details, string Recommendation)> VerifyDocumentAsync(byte[] imageBytes)
        {
            var prompt = "Identify the document in the image. Is it a valid student ID card or an academic credential? Extract the student name and institution if visible. Respond with whether it is 'valid' or 'invalid' and provide details.";
            
            return await _ollama.AnalyzeImageAsync(imageBytes, prompt, "moondream");
        }
    }
}
