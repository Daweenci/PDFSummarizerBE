using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIResponse
{
    public class SummaryTopic
    {
        [JsonPropertyName("topic")]
        public required string Topic { get; set; }

        [JsonPropertyName("points")]
        public required List<string> Points { get; set; }
    }
}
