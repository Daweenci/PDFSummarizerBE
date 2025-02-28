using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIResponse
{
    public class PromptTokensDetails
    {
        [JsonPropertyName("cached_tokens")]
        public int CachedTokens { get; set; }
    }
}
