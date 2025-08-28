using SumarizerService.Models.OpenAIResponse;
using System.Text.Json;
using System.Text.Json.Serialization;
using SumarizerService.Models;

namespace SumarizerService.Extensions
{
    public class MessageConverter : JsonConverter<Message>
    {
        public override Message Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var role = root.GetProperty("role").GetString();

            var contentJson = root.GetProperty("content").GetString();

            if (role is null || contentJson is null)
            {
                throw new JsonException();
            }

            var summaryResponse = JsonSerializer.Deserialize<SummaryResponse>(contentJson, options);

            if (summaryResponse is null)
            {
                throw new JsonException();
            }

            return new Message
            {
                Role = role,
                Content = summaryResponse
            };
        }

        public override void Write(Utf8JsonWriter writer, Message value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("role", value.Role);
            writer.WriteString("content", JsonSerializer.Serialize(value.Content, options));
            writer.WriteEndObject();
        }
    }

}
