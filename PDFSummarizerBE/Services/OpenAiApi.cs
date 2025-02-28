using SumarizerService.Models;
using SumarizerService.Models.OpenAIRequest;
using SumarizerService.Models.OpenAIResponse;
using System.Text;
using System.Text.Json;

namespace PDFSummarizerBE.Services
{
    public class OpenAiApi
    {
        private const string modelName = "gpt-4o-mini";
        private const string deepSeekEndpoint = "https://api.openai.com/v1/chat/completions";
        private string instruction = """Du bist ein Experte für akademische Zusammenfassungen. Deine Aufgabe ist es, den gegebenen Text vollständig und detailliert zusammenzufassen, ohne dass relevante Informationen verloren gehen. Alle Konzepte, Begriffe und Theorien müssen vollständig und verständlich erklärt werden, auch wenn im Text keine direkte Erklärung dafür vorhanden ist. Falls ein Konzept oder Begriff ohne Erklärung auftaucht, füge eine vollständige und verständliche Erklärung hinzu, einschließlich praktischer Beispiele, wenn dies hilft, das Verständnis zu vertiefen. Es dürfen keine Themen ausgelassen oder stark verkürzt werden, da die Zusammenfassung zum Lernen für eine Klausur verwendet werden soll und daher genauso vollständig und informativ sein muss wie der Originaltext. Die Antwort muss lang genug sein, um alle wichtigen Details zu behandeln, und eine klare Struktur im JSON-Format aufweisen: {"summary":[{"topic": "Themenname", "points": ["Relevante Erklärung 1", "Relevante Erklärung 2", ...]}]} Achte darauf, dass auch komplexe Themen vollständig und detailliert erklärt werden, ohne sie zu verallgemeinern oder zu vereinfachen. Ergänze, wo möglich, praktische Beispiele, um das Verständnis der Konzepte zu fördern. Die Zusammenfassung muss alle Informationen enthalten, die für das Verständnis der Konzepte notwendig sind, und sollte als vollständige Lernressource dienen.""";
        private string apiKey;
        private readonly HttpClient _httpClient;

        private OpenAIMessage[] messages = new OpenAIMessage[2];
        
        public OpenAiApi(string apiKey)
        {
            this.apiKey = apiKey;
            this._httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };

            this.messages[0] = new OpenAIMessage(Role.system, this.instruction);
        }

        public async Task<SummaryResponse> SummarizeLargeText(string longText)
        {
            int maxTokensPerRequest = 30000;
            var textChunks = SplitTextIntoChunks(longText, maxTokensPerRequest);
            var summaries = new List<SummaryResponse>();

            foreach (var chunk in textChunks)
            {
                summaries.Add(await SummarizeText(chunk));
            }

            return MergeSummaries(summaries);
        }

        public SummaryResponse MergeSummaries(List<SummaryResponse> summaries)
        {
            var mergedSummary = new SummaryResponse
            {
                Summary = new List<SummaryTopic>()
            };

            foreach (var summary in summaries)
            {
                foreach (var topic in summary.Summary)
                {
                    var existingTopic = mergedSummary.Summary.FirstOrDefault(t => t.Topic == topic.Topic);
                    if (existingTopic == null)
                    {
                        mergedSummary.Summary.Add(topic);
                    }
                    else
                    {
                        existingTopic.Points.AddRange(topic.Points);
                    }
                }
            }

            return mergedSummary;
        }

        private List<string> SplitTextIntoChunks(string text, int maxTokens)
        {
            var words = text.Split(' ');
            var chunks = new List<string>();
            var currentChunk = new List<string>();
            int tokenCount = 0;

            foreach (var word in words)
            {
                tokenCount += EstimateTokens(word);
                if (tokenCount > maxTokens)
                {
                    chunks.Add(string.Join(" ", currentChunk));
                    currentChunk.Clear();
                    tokenCount = EstimateTokens(word);
                }
                currentChunk.Add(word);
            }
            if (currentChunk.Count > 0) chunks.Add(string.Join(" ", currentChunk));

            return chunks;
        }

        private int EstimateTokens(string word) => word.Length / 3; // Rough estimate (1 token = ~3 characters)

        public async Task<SummaryResponse> SummarizeText(string text) {
            if (text == null || String.IsNullOrEmpty(text))
            {
                throw new ArgumentException("Eingabetext darf nicht null oder empty sein.");
            }

            this.messages[1] = new OpenAIMessage(Role.user, text);

            OpenAIRequest requestBody = new()
            {
                Model = OpenAiApi.modelName,
                Messages = this.messages
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            this._httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this.apiKey);

            try
            {
                var response = await this._httpClient.PostAsync(OpenAiApi.deepSeekEndpoint, content);
                if(!response.IsSuccessStatusCode)
                {
                    var responseString2 = await response.Content.ReadAsStringAsync();
                    throw new Exception(responseString2);
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var respnseStringWithoutNewLines = responseString.Replace("\n", "").Replace("\r", "");
                var jsonResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseString);

                if (jsonResponse?.Choices == null || jsonResponse.Choices.Count == 0)
                    throw new Exception("Invalid API response: no choices returned.");

                return jsonResponse.Choices[0].Message.Content;
            }
            catch
            {
                throw;
            }
        }
    }
}
