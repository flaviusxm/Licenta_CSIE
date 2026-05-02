using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
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
            // 1. ADVERSARIAL DEFENSE LAYER: Normalization
            var normalizedContent = NormalizeText(content);
            var normalizedTitle = title != null ? NormalizeText(title) : null;

            // 2. FEW-SHOT LEARNING PROMPT
            var prompt = @"Ești Guardian Shield, un sistem avansat de moderare pentru o platformă academică. 
            Analizează conținutul pentru: limbaj licențios, hărțuire, sau comportament non-academic.
            
            EXEMPLE (Few-Shot):
            - ""Salut, cine mă poate ajuta la Analiză?"" -> { ""isSafe"": true, ""reason"": ""Interacțiune academică legitimă"", ""confidence"": 1.0 }
            - ""Ești un i.d.i.o.t și nu știi nimic"" -> { ""isSafe"": false, ""reason"": ""Hărțuire și limbaj jignitor (detectat prin normalizare)"", ""confidence"": 0.95 }
            - ""Vând proiecte de licență la preț mic"" -> { ""isSafe"": false, ""reason"": ""Spam / Fraudă academică"", ""confidence"": 0.9 }

            Răspunde DOAR în format JSON: { ""isSafe"": boolean, ""reason"": ""explicație detaliată pentru admin"", ""confidence"": float }";
            
            var result = await _ollama.AnalyzeTextAsync(normalizedContent, prompt, "qwen2.5:0.5b");
            return (result.IsSafe, result.Reason);
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Eliminăm caracterele de tip separator folosite pentru a ascunde cuvinte (ex: s.t.u.p.i.d -> stupid)
            // Dar păstrăm spațiile dintre cuvintele normale
            var normalized = Regex.Replace(text, @"(?<=[a-zA-Z])[\.\-\_\*](?=[a-zA-Z])", "");

            // Leetspeak translation (minimală pentru demonstrație)
            normalized = normalized.Replace("0", "o").Replace("1", "i").Replace("3", "e").Replace("4", "a").Replace("5", "s").Replace("7", "t");

            return normalized.ToLower();
        }

        public async Task<(bool IsValid, string Details, string Recommendation)> VerifyDocumentAsync(byte[] imageBytes)
        {
            // 3. HUMAN-IN-THE-LOOP (HITL) PROMPT for Vision
            var prompt = @"Identify the document. We need to verify if this is a real student ID or carnet.
            Extract: Name, Institution, and Expiry Date if visible.
            Provide a recommendation for the Human Admin: 'Approve', 'Reject', or 'Manual Review Needed'.
            Reasoning: Explain why (e.g. 'Photo is blurry', 'Name matches user profile', 'Institution not recognized').";
            
            return await _ollama.AnalyzeImageAsync(imageBytes, prompt, "moondream");
        }
    }
}
