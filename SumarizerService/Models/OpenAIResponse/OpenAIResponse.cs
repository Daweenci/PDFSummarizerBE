using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIResponse
{
    public class OpenAIResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; }

        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }

        [JsonPropertyName("system_fingerprint")]
        public string SystemFingerprint { get; set; }
    }
}
