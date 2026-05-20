using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using AskNLearn.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AskNLearn.Infrastructure.Services
{
    public class TrustLayerService : ITrustLayerService
    {
        private readonly ILocalAiEngine _ollama;
        private readonly ILogger<TrustLayerService> _logger;

        public TrustLayerService(ILocalAiEngine ollama, ILogger<TrustLayerService> logger)
        {
            _ollama = ollama;
            _logger = logger;
        }

        public async Task<(bool IsSafe, string Reason)> ModerateTextAsync(string content, string? title = null)
        {
            var normalizedContent = NormalizeText(content);
            var normalizedTitle = title != null ? NormalizeText(title) : null;

            // Prompt mai scurt — qwen2.5:0.5b e mic, prompt-urile lungi îl confuzionează
            var prompt = @"You are a content moderator for a student academic platform.
Analyze the text for: harassment, offensive language, spam, academic fraud, or illegal content.
Reply ONLY with valid JSON, nothing else: {""isSafe"": true/false, ""reason"": ""brief explanation""}";

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
            // Prompt avansat pentru Llama 3.2 Vision - specializat pe structura documentelor românești
            var prompt = "You are an advanced AI document verification agent specializing in Romanian documents. " +
                         "Analyze the image (Romanian Identity Card or Student ID/Legitimație student). " +
                         "CRITICAL RULES: " +
                         "1. For Student IDs: The 'Full Name' is HANDWRITTEN in the middle, below 'Nume și prenume student'. " +
                         "2. The 'Institution' (University) is at the TOP, near 'Instituția de învățământ' (e.g., ASE BUCURESTI). " +
                         "3. DO NOT confuse the University name with the Student Name. " +
                         "4. Extract the 13-digit CNP carefully. " +
                         "Write a POSITIVE, professional report. Extract: Document Type, Full Name (Student Name, NOT University), CNP, and Institution. " +
                         "If handwritten text is hard to read, do your best (e.g., look for 'PIRJOLEANU'). Confidence Score (0-100).";
            
            var ocrResult = await _ollama.AnalyzeImageAsync(imageBytes, prompt, "llama3.2-vision");

            if (string.IsNullOrWhiteSpace(ocrResult.Details) || ocrResult.Details.Contains("Empty vision response") || ocrResult.Details.Contains("Vision API error"))
            {
                _logger.LogWarning("[TrustLayer] Moondream returned empty or error response. Routing to manual review.");
                return (false, "AI OCR failed to read document. Manual review required.", "Review Required");
            }

            // OllamaService.AnalyzeImageAsync face deja keyword matching pe răspunsul liber
            // Folosim direct rezultatul fără un al doilea layer heuristic
            string formattedDetails = $"[AI Analysis - Llama 3.2 Vision]:\n\n{ocrResult.Details}\n\n[System Status]: {(ocrResult.IsValid ? "Pass" : "Needs Review")}";
            string recommendation = ocrResult.IsValid ? "Approved" : "Manual Review Needed";

            _logger.LogInformation("[TrustLayer] Document verification result: {Status}. Details: {Details}", ocrResult.IsValid, ocrResult.Details);

            return (ocrResult.IsValid, formattedDetails, recommendation);
        }
    }
}
