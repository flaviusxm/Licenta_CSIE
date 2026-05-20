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
    public class LocalAiEngine : ILocalAiEngine
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LocalAiEngine> _logger;
        private const string OllamaUrl = "http://localhost:11434/api/generate";

        public LocalAiEngine(HttpClient httpClient, ILogger<LocalAiEngine> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromMinutes(5); // Mărit pentru modele mari de viziune (ex: LLava) care rulează pe CPU
        }

        private async Task<byte[]> PreprocessImageAsync(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _logger.LogWarning("[Ollama] PreprocessImageAsync received empty or null image bytes. Skipping preprocessing.");
                return Array.Empty<byte>();
            }

            try
            {
                // FIX: Use Stream instead of byte[] directly — compatible with .NET 10 + ImageSharp 3.x
                using var inputStream = new MemoryStream(imageBytes);
                using var image = await Image.LoadAsync(inputStream);
                
                // 0. Auto-Orient: Rezolvă problema pozelor rotite (EXIF data) de pe telefoane
                image.Mutate(x => x.AutoOrient());

                // 1. Resize optimization: 1200px pentru detalii fine fără a supraîncărca VRAM-ul
                int maxWidth = 1200;
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(maxWidth, maxWidth),
                    Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max
                }));

                // 2. Ultra-Enhance for Vision: Pipeline de claritate extremă
                image.Mutate(x => x
                    .Contrast(1.6f)      // Contrast ridicat pentru a separa textul de fundal
                    .GaussianSharpen(2.2f) // Sharpening agresiv pentru caractere mici
                    .Brightness(1.05f));   // Uniformizare luminozitate

                using var ms = new MemoryStream();
                // Moondream is much more stable with standard PNGs
                await image.SaveAsPngAsync(ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Ollama] Could not preprocess image, sending raw bytes.");
                return imageBytes;
            }
        }

        public async Task<(bool IsSafe, string Reason)> AnalyzeTextAsync(string content, string prompt, string model = "llama3.2")
        {
            try
            {
                var requestBody = new
                {
                    model = model,
                    prompt = $"{prompt}\n\nText to analyze: {content}",
                    stream = false,
                    format = "json"
                };

                var response = await _httpClient.PostAsJsonAsync(OllamaUrl, requestBody);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var responseText = jsonResponse.GetProperty("response").GetString();

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("[Ollama] Empty text moderation response. Defaulting to manual review (not safe).");
                    return (false, "AI returned empty response - flagged for review.");
                }

                // Extrage JSON chiar dacă modelul adaugă text extra
                var jsonMatch = System.Text.RegularExpressions.Regex.Match(responseText, @"\{.*?\}", System.Text.RegularExpressions.RegexOptions.Singleline);
                var jsonToParse = jsonMatch.Success ? jsonMatch.Value : responseText;

                try
                {
                    var result = JsonSerializer.Deserialize<ModerationResult>(jsonToParse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return (result?.IsSafe ?? false, result?.Reason ?? "No reason provided.");
                }
                catch (JsonException)
                {
                    // Dacă JSON-ul e complet invalid, analizăm textul liber
                    _logger.LogWarning("[Ollama] Could not parse JSON from text moderation. Raw: {Raw}", responseText);
                    var lowerText = responseText.ToLowerInvariant();
                    bool likelySafe = !lowerText.Contains("unsafe") && !lowerText.Contains("false") && !lowerText.Contains("violation");
                    return (likelySafe, $"AI raw response (parse failed): {responseText[..Math.Min(200, responseText.Length)]}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Ollama for text moderation.");
                // IMPORTANT: Nu aproba automat la eroare — trimite la review manual
                return (false, $"AI processing error - flagged for manual review: {ex.Message}");
            }
        }

        public async Task<(bool IsValid, string Details, string Recommendation)> AnalyzeImageAsync(byte[] imageBytes, string prompt, string model = "llama3.2-vision")
        {
            // Guard — empty array → Moondream returns 500
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _logger.LogError("[Ollama] AnalyzeImageAsync called with empty imageBytes. Aborting.");
                return (false, "No image data received — file may be missing from server.", "Review Required");
            }

            int maxRetries = 2;
            int attempt = 0;

            // Pre-procesăm imaginea o singură dată
            var processedBytes = await PreprocessImageAsync(imageBytes);

            // If preprocessing returned empty (failed to load image), abort
            if (processedBytes.Length == 0)
            {
                _logger.LogError("[Ollama] Image preprocessing returned 0 bytes. Original size was {Size}. Aborting.", imageBytes.Length);
                return (false, "Image preprocessing failed — file may be corrupted.", "Review Required");
            }

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

                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        if (attempt < maxRetries)
                        {
                            _logger.LogWarning("[Ollama] Empty response on attempt {Attempt}. Retrying...", attempt);
                            await Task.Delay(2000);
                            continue;
                        }
                        return (false, "Empty vision response after retries.", "Review Required");
                    }

                    _logger.LogDebug("[Ollama] Raw AI Response: {Response}", responseText);

                    // Analiză keyword pe răspunsul liber text al AI-ului (LLava)
                    var lowerResponse = responseText.ToLowerInvariant();
                    
                    // Încercăm să extragem un scor de încredere dacă modelul l-a furnizat
                    int confidence = 0;
                    var confidenceMatch = System.Text.RegularExpressions.Regex.Match(lowerResponse, @"(\d+)(?:\s*%|/100| confidence)");
                    if (confidenceMatch.Success)
                    {
                        int.TryParse(confidenceMatch.Groups[1].Value, out confidence);
                    }

                    // Listă extinsă bazată pe câmpurile reale de pe un buletin românesc și carduri de student
                    var keywords = new[] { 
                        "student", "university", "college", "facultate", "identitate", "identity", "idrou", "seria", "cnp", "romania", "idsignature",
                        "carte d'identite", "carte de identitate", "identity card", "nume", "nom", "last name", "legitimație", "legitimatie", "educatiei",
                        "prenume", "prenom", "first name", "cetatenie", "nationalite", "nationality",
                        "loc nastere", "lieu de naissance", "place of birth", "domiciliu", "adresse", "address",
                        "emisa de", "delivree par", "issued by", "valabilitate", "validite", "validity",
                        "românia", "ministerul", "afacerilor", "interne"
                    };

                    var foundKeywords = keywords.Where(k => lowerResponse.Contains(k)).ToList();
                    if (System.Text.RegularExpressions.Regex.IsMatch(lowerResponse, @"\bid\b") || lowerResponse.Contains("national id")) foundKeywords.Add("id");

                    bool isValid = foundKeywords.Any();

                    // Verificăm dacă AI-ul a identificat tipul documentului (ID sau Student) - Acest lucru crește precizia validării
                    bool isDocTypeIdentified = lowerResponse.Contains("identity card") || 
                                              lowerResponse.Contains("student id") || 
                                              lowerResponse.Contains("carte de identitate") ||
                                              lowerResponse.Contains("romania");

                    if (isDocTypeIdentified && foundKeywords.Count >= 1) isValid = true; // Dacă știm ce este și am găsit măcar un element (nume/CNP), e valid

                    // Dacă avem un scor de încredere mare (>80), îl considerăm valid chiar dacă keyword-urile sunt ambigue
                    if (confidence >= 80) isValid = true;
                    if (confidence > 0 && confidence < 40) isValid = false;

                    // Construim un raport detaliat pentru Admin Console
                    var report = new System.Text.StringBuilder();
                    report.AppendLine("[GUARDIAN SHIELD AI ANALYSIS REPORT]");
                    report.AppendLine($"Confidence Score: {(confidence > 0 ? confidence.ToString() + "%" : "N/A")}");
                    report.AppendLine($"System Status: {(isValid ? "PASS (Verified)" : "FLAGGED (Needs Manual Review)")}");
                    report.AppendLine();
                    report.AppendLine("[Identified Document Elements]:");
                    if (foundKeywords.Any())
                        report.AppendLine(string.Join(", ", foundKeywords.Select(k => $"'{k}'")));
                    else
                        report.AppendLine("None detected.");
                    
                    report.AppendLine();
                    report.AppendLine("[Raw Vision Response]:");
                    report.AppendLine(responseText);
                    report.AppendLine();
                    report.AppendLine("[Decision Logic]:");
                    report.AppendLine($"- Keyword Count: {foundKeywords.Count}");
                    report.AppendLine($"- AI Confidence: {confidence}%");
                    report.AppendLine($"- Requirement: At least 1 keyword OR confidence > 80%");
                    report.AppendLine($"- Final Result: {(isValid ? "APPROVED" : "REJECTED/REVIEW")}");

                    return (isValid, report.ToString(), isValid ? "Approved" : "Needs Manual Review");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Ollama] Critical error during vision analysis (Attempt {Attempt}).", attempt);
                    if (attempt >= maxRetries) 
                    {
                        return (false, "Vision API error: " + ex.Message, "Review Required");
                    }
                    await Task.Delay(2000);
                }
            }

            return (false, "AI failed to respond entirely.", "Needs Manual Review");
        }

        private class ModerationResult
        {
            public bool IsSafe { get; set; }
            public string Reason { get; set; } = string.Empty;
        }
    }
}
