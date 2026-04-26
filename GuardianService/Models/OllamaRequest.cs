using System.Text.Json.Serialization;

namespace GuardianService.Models
{
    public class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("images")]
        public string[]? Images { get; set; }

        [JsonPropertyName("options")]
        public Dictionary<string, object>? Options { get; set; }
    }
}
