using System.Text.Json.Serialization;

namespace SumarizerService.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Role
    {
        system,
        user
    }
}
