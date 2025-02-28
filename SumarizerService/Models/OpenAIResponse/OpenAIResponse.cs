using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIResponse
{
    public class OpenAIResponse
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("object")]
        public required string Object { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public required string Model { get; set; }

        [JsonPropertyName("choices")]
        public required List<Choice> Choices { get; set; }

        [JsonPropertyName("usage")]
        public required Usage Usage { get; set; }

        [JsonPropertyName("system_fingerprint")]
        public required string SystemFingerprint { get; set; }
    }
}
