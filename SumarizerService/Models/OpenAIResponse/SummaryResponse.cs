using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIResponse
{
    public class SummaryResponse
    {
        [JsonPropertyName("summary")]
        public List<SummaryTopic> Summary { get; set; }
    }
}
