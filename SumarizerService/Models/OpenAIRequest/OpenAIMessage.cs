using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIRequest
{
    public class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public Role Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        public OpenAIMessage(Role role, string content)
        {
            Role = role;
            Content = content;
        }
    }
}
