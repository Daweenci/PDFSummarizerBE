using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIResponse
{
    public class SummaryResponse
    {
        [JsonPropertyName("summary")]
        public required List<SummaryTopic> Summary { get; set; }
    }
}
