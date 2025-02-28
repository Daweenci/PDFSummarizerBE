using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIResponse
{
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public SummaryResponse Content { get; set; }
    }
}
