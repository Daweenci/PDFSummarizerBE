using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIResponse
{
    public class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public required Message Message { get; set; }

        [JsonPropertyName("logprobs")]
        public required object LogProbs { get; set; }

        [JsonPropertyName("finish_reason")]
        public required string FinishReason { get; set; }
    }
}
