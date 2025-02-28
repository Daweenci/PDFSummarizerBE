using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIRequest
{
    public class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public required Role Role { get; set; }

        [JsonPropertyName("content")]
        public required string Content { get; set; }
    }
}
