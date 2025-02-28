using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIResponse
{
    public class SummaryTopic
    {
        [JsonPropertyName("topic")]
        public string Topic { get; set; }

        [JsonPropertyName("points")]
        public List<string> Points { get; set; }
    }
}
