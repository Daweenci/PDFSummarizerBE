using System.Text;
using System.Text.Json;

namespace PDFSummarizerBE.Services
{
    public class OpenAiApi
    {
        private string apiKey;
        private const string deepSeekEndpoint = "https://api.deepseek.com/chat/completions";
        private readonly HttpClient _httpClient;

        private string instruction = "Fasse den Text so kurz wie möglich " +
            "zusammen, ohne jeglichen Inhalt zu verlieren, es geht um eine Themenzusammenfassung. " +
            "Formatiere es wie Folgt: das übergeordnete Thema als Überschrifft und den Inhalt darunter " +
            "als Fliesstext. Lasse zwischen den Themen einen Abstand";

        private OpenAIMessage[] messages = new OpenAIMessage[2];

        public OpenAiApi(string apiKey)
        {
            this.apiKey = apiKey;
            this._httpClient = new HttpClient();

            this.messages[0] = new OpenAIMessage(Role.system, this.instruction);
        }

        public async Task<string> SummarizeText(string text) {
            if (text == null || String.IsNullOrEmpty(text))
            {
                throw new ArgumentException("Eingabetext darf nicht null oder empty sein.");
            }

            this.messages[1] = new OpenAIMessage(Role.user, text);

            var requestBody = new
            {
                model = "deepseek-chat",
                messages = this.messages,
                response_format = new
                {
                    type = "json_object"
                }
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            this._httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this.apiKey);

            try
            {
                var response = await this._httpClient.PostAsync(OpenAiApi.deepSeekEndpoint, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<DeepSeekResponse>(responseString);

                return jsonResponse?.Choices?[0].Message?.Content ?? "No summary provided";
            }
            catch
            {
                throw;
            }
        }
    }

    public class OpenAIMessage(Role Role, string Content)
    {
        private Role role = Role;
        private string content = Content;
    }

    public enum Role
    {
        system, 
        user
    }

    public class DeepSeekResponse
    {
        public List<DeepSeekChoice> Choices { get; set; }
    }

    public class DeepSeekChoice
    {
        public DeepSeekMessage Message { get; set; }
    }

    public class DeepSeekMessage
    {
        public string Content { get; set; }
    }
}
