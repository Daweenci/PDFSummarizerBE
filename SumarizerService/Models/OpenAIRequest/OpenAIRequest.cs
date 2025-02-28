using System.Text.Json.Serialization;

namespace SumarizerService.Models.OpenAIRequest
{
    public class OpenAIRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }   //TODO: In Future change to Enum, once we know all the models

        [JsonPropertyName("messages")]
        public OpenAIMessage[] Messages { get; set; }

        [JsonPropertyName("response_format")]
        public object ResponseFormat { get; set; }
    }
}
