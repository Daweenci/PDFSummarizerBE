using Microsoft.Extensions.Logging;
using SumarizerService.Middleware;
using SumarizerService.Models;
using SumarizerService.Models.OpenAIRequest;
using SumarizerService.Models.OpenAIResponse;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace SumarizerService.Core
{
    public class OpenAISummarizerService : SummarizerService
    {
        #region public properties
        public override string ModelName => "gpt-4o-mini";
        public override Uri Endpoint => new("https://api.openai.com/v1/chat/completions");
        #endregion

        #region private properties
        /// <summary>
        /// Rough estimate (1 token = ~3 characters)
        /// </summary>
        private readonly int _maxTokensPerRequest = 10000;
        private readonly string _instruction = """Du bist ein Experte für akademische Zusammenfassungen. Deine Aufgabe ist es, den gegebenen Text vollständig und detailliert zusammenzufassen, ohne dass relevante Informationen verloren gehen. Alle Konzepte, Begriffe und Theorien müssen vollständig und verständlich erklärt werden, auch wenn im Text keine direkte Erklärung dafür vorhanden ist. Falls ein Konzept oder Begriff ohne Erklärung auftaucht, füge eine vollständige und verständliche Erklärung hinzu, einschließlich praktischer Beispiele, wenn dies hilft, das Verständnis zu vertiefen. Es dürfen keine Themen ausgelassen oder stark verkürzt werden, da die Zusammenfassung zum Lernen für eine Klausur verwendet werden soll und daher genauso vollständig und informativ sein muss wie der Originaltext. Die Antwort muss lang genug sein, um alle wichtigen Details zu behandeln, und eine klare Struktur im JSON-Format aufweisen: {"summary":[{"topic": "Themenname", "points": ["Relevante Erklärung 1", "Relevante Erklärung 2", ...]}]} Achte darauf, dass auch komplexe Themen vollständig und detailliert erklärt werden, ohne sie zu verallgemeinern oder zu vereinfachen. Ergänze, wo möglich, praktische Beispiele, um das Verständnis der Konzepte zu fördern. Die Zusammenfassung muss alle Informationen enthalten, die für das Verständnis der Konzepte notwendig sind, und sollte als vollständige Lernressource dienen.""";
        private readonly IApiKeyProvider _apiKeyProvider;
        private readonly HttpClient _httpClient;
        private readonly OpenAIMessage[] _messages = new OpenAIMessage[2];
        private readonly ILogger<OpenAISummarizerService> _logger;
        #endregion

        #region constructor
        public OpenAISummarizerService(IApiKeyProvider apiKeyProvider, ILogger<OpenAISummarizerService> logger)
        {
            this._apiKeyProvider = apiKeyProvider;
            this._logger = logger;
            this._httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5),
                DefaultRequestHeaders =
                {
                    Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this._apiKeyProvider.ApiKey)
                }
            };

            this._messages[0] = new OpenAIMessage { Role = Role.system, Content = this._instruction };
        }
        #endregion

        #region public methods
        /// <summary>
        /// <para>Summarizes the given text using the OpenAI API.</para>
        /// <para>Splits the text into 30k token chunks and merges them back together in the end, this is for safety such that the AI
        ///       doesn't run into RateLimiting and doesn't skip topics that are important</para>
        /// </summary>
        /// <param name="text">The text to summarize</param>
        /// <returns>the summarized text</returns>
        public override async Task<SummaryResponse> SummarizeText(string text)
        {
            List<string> textChunks = this.SplitTextIntoChunks(text);
            List<SummaryResponse> summaries = [];

            foreach (string chunk in textChunks)
            {
                string[] alreadySummarizedTopics = [.. summaries
                    .SelectMany(s => s.Summary.Select(t => t.Topic))
                    .Distinct()];

                summaries.Add(await SendSummaryRequestToAPI(chunk, alreadySummarizedTopics));
            }

            return MergeSummaries(summaries);
        }
        #endregion

        #region private methods
        private async Task<SummaryResponse> SendSummaryRequestToAPI(string text, string[] alreadySummarizedTopics)
        {
            ValidateInput(text);
            UpdateUserMessage(text, alreadySummarizedTopics);

            OpenAIRequest requestBody = CreateOpenAIRequest();
            string responseString = await SendPostRequestAsync(this.Endpoint, requestBody);

            return ParseResponse(responseString);
        }

        private static void ValidateInput(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Eingabetext darf nicht null oder empty sein.");
            }
        }

        private void UpdateUserMessage(string textToSummarize, string[] alreadySummarizesTopics)
        {
            // The message we send will not only include the textToSummarize but also the already summarized topics
            // This is to make the AI know which topics are already summarized and which are not
            string alreadySummarizedTopicsText = string.Join(" ", alreadySummarizesTopics);
            string messageContent = $"Ich habe bereits folgende Themen zusammengefasst, falls du inhalt findest, welcher sich logisch einen der themen anschließt, dann fasse ihn unter dem gleichen Titel zusammen. {alreadySummarizedTopicsText}";

            // If there are already summarized topics, we need to include them in the message otherwise we just send the text to summarize
            string messageToSend = alreadySummarizesTopics.Length != 0 ? $"{messageContent} {textToSummarize}" : textToSummarize;

            this._messages[1] = new OpenAIMessage { Role = Role.user, Content = messageToSend };
        }

        private OpenAIRequest CreateOpenAIRequest()
        {
            return new OpenAIRequest
            {
                Model = this.ModelName,
                Messages = this._messages,
                ResponseFormat = new { type = "json_object" }
            };
        }

        private async Task<string> SendPostRequestAsync(Uri url, OpenAIRequest requestBody)
        {
            string requestJson = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            int maxRetries = 5; // Maximum number of retries before failing
            int delayMilliseconds = 60000; // 1 minute initial delay

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                using HttpResponseMessage response = await this._httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // 429 Rate Limited
                {
                    _logger.LogDebug("Rate limited. Retrying in {DelaySeconds} seconds...", delayMilliseconds / 1000);
                    await Task.Delay(delayMilliseconds);
                }
                else
                {
                    response.EnsureSuccessStatusCode(); // If not rate limited, throw exception
                }
            }

            throw new Exception("Max retries reached due to rate limiting.");
        }

        private static SummaryResponse ParseResponse(string responseString)
        {
            var sanitizedResponse = responseString.Replace("\n", "").Replace("\r", "");
            var jsonResponse = JsonSerializer.Deserialize<OpenAIResponse>(sanitizedResponse);

            if (jsonResponse?.Choices == null || jsonResponse.Choices.Count == 0)
            {
                throw new Exception("Invalid API response: no choices returned.");
            }

            return jsonResponse.Choices[0].Message.Content;
        }

        private static SummaryResponse MergeSummaries(List<SummaryResponse> summaries)
        {
            var mergedSummary = new SummaryResponse
            {
                Summary = []
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

        private List<string> SplitTextIntoChunks(string text)
        {
            var words = text.Split(' ');
            var chunks = new List<string>();
            var currentChunk = new List<string>();
            int tokenCount = 0;

            foreach (var word in words)
            {
                tokenCount += EstimateTokens(word);
                if (tokenCount > this._maxTokensPerRequest)
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
        #endregion
    }
}
