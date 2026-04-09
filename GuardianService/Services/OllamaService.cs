using System.Net.Http.Json;
using System.Text.Json;
using GuardianService.DTOs;

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

        public OllamaService(HttpClient httpClient, IConfiguration config, ILogger<OllamaService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<ModerationResponse> AnalyzeTextAsync(string content, string? title = null)
        {
            var model = _config["Ollama:TextModel"] ?? "llama3.1";
            var baseUrl = _config["Ollama:BaseUrl"] ?? "http://localhost:11434";

            var prompt = $@"Analizează următorul conținut pentru o platformă socială de studenți. 
Căutăm: limbaj licențios, hărțuire, spam sau conținut ilegal.

Titlu: {title ?? "N/A"}
Conținut: {content}

Răspunde DOAR în format JSON:
{{
  ""isSafe"": boolean,
  ""reason"": ""scurtă explicație în Română""
}}";

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/generate", new
                {
                    model = model,
                    prompt = prompt,
                    stream = false,
                    format = "json"
                });

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

            // Prompt pentru verificarea legitimației de student
            var prompt = "Este aceasta o legitimație de student validă? Extrage numele studentului, facultatea și data expirării dacă sunt vizibile. Răspunde în format JSON: {\"isValid\": boolean, \"extractionDetails\": \"string\", \"recommendation\": \"string\"}";

            try
            {
                var requestBody = new
                {
                    model = model,
                    prompt = prompt,
                    stream = false,
                    format = "json",
                    images = base64Image != null ? new[] { base64Image } : null
                };

                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/generate", requestBody);
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
