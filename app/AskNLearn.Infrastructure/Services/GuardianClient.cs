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
            var normalized = Regex.Replace(text, @"(?<=[a-zA-Z])[.\-_*](?=[a-zA-Z])", "");

            // Leetspeak translation (minimală pentru demonstrație)
            normalized = normalized.Replace("0", "o").Replace("1", "i").Replace("3", "e").Replace("4", "a").Replace("5", "s").Replace("7", "t");

            return normalized.ToLower();
        }

        public async Task<(bool IsValid, string Details, string Recommendation)> VerifyDocumentAsync(byte[] imageBytes)
        {
            // Pasul 1: AI-ul Moondream este folosit STRICT pentru OCR (extragere text)
            var prompt = "Read all the text visible in this document.";
            var ocrResult = await _ollama.AnalyzeImageAsync(imageBytes, prompt, "moondream");

            if (string.IsNullOrEmpty(ocrResult.Details) || ocrResult.Details.Contains("Empty vision response"))
            {
                return (false, "AI OCR failed to read document.", "Review Required");
            }

            string extractedText = ocrResult.Details.ToLowerInvariant();
            
            // Pasul 2: Algoritm Heuristic de Calculare a Probabilității
            int score = 0;
            int maxScore = 100;
            
            // Verificăm markeri de identitate (România)
            if (extractedText.Contains("carte de identitate") || extractedText.Contains("romania") || extractedText.Contains("cnp")) score += 30;
            if (System.Text.RegularExpressions.Regex.IsMatch(extractedText, @"[0-9]{13}")) score += 20; // Detecție CNP (13 cifre)
            if (extractedText.Contains("seria") || System.Text.RegularExpressions.Regex.IsMatch(extractedText, @"[a-z]{2}\s?[0-9]{6}")) score += 10;
            
            // Verificăm markeri academici
            if (extractedText.Contains("student") || extractedText.Contains("carnet") || extractedText.Contains("legitimatie")) score += 20;
            if (extractedText.Contains("universita") || extractedText.Contains("faculta") || extractedText.Contains("academ")) score += 20;

            // Ajustare finală (capped at 100)
            score = Math.Min(score, maxScore);

            bool isValid = score >= 60; // Threshold de 60% pentru aprobare
            string recommendation = isValid ? "Approved" : "Manual Review Needed";
            string formattedDetails = $"[OCR Output]: {ocrResult.Details}\n[Confidence Score]: {score}%\n[System Status]: {(isValid ? "Pass" : "High Risk")}";

            return (isValid, formattedDetails, recommendation);
        }
    }
}
