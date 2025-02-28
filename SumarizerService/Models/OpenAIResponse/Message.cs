using SumarizerService.Extensions;
using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIResponse
{
    [JsonConverter(typeof(MessageConverter))]
    public class Message
    {
        [JsonPropertyName("role")]
        public required string Role { get; set; }

        [JsonPropertyName("content")]
        public required SummaryResponse Content { get; set; }
    }
}
