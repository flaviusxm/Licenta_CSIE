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
        private const string ModelName = "llama3.1"; // Can be configurable
        private const string OllamaUrl = "http://localhost:11434/api/generate";

        public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<(bool IsSafe, string Reason)> AnalyzeContentAsync(string content, string? title = null)
        {
            try
            {
                var prompt = $@"Analizează următorul conținut pentru platforma universitară AskNLearn. 
Căutăm: 
1. Amenințări de securitate (XSS, SQL Injection, cod malițios).
2. Încălcări de conținut (limbaj licențios, hărțuire, spam).

Titlu: {title ?? "N/A"}
Conținut: {content}

Răspunde DOAR în format JSON:
{{
  ""isSafe"": boolean,
  ""reason"": ""scurtă explicație în Română""
}}";

                var requestBody = new
                {
                    model = ModelName,
                    prompt = prompt,
                    stream = false,
                    format = "json"
                };

                var response = await _httpClient.PostAsJsonAsync(OllamaUrl, requestBody);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var responseText = jsonResponse.GetProperty("response").GetString();

                if (string.IsNullOrEmpty(responseText))
                {
                    return (true, "Nu s-a putut analiza conținutul (răspuns gol).");
                }

                var result = JsonSerializer.Deserialize<ModerationResult>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return (result?.IsSafe ?? true, result?.Reason ?? "Analiză finalizată.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Ollama API for moderation.");
                return (true, "Eroare la procesarea AI. Conținutul a fost aprobat implicit.");
            }
        }

        private class ModerationResult
        {
            public bool IsSafe { get; set; }
            public string Reason { get; set; } = string.Empty;
        }
    }
}
