using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SumarizerService.Extensions;
using SumarizerService.Middleware;
using SumarizerService.Models;
using SumarizerService.Models.OpenAIRequest;
using SumarizerService.Models.OpenAIResponse;
using static System.Net.WebRequestMethods;

namespace SumarizerService.Core
{
    public class GeminiSummarizerService : SummarizerService
    {

        #region private properties
        private readonly int _maxTokensPerRequest = 10000;
        private readonly string _instruction = """Du bist ein Experte für akademische Zusammenfassungen. Deine Aufgabe ist es, den gegebenen Text vollständig und detailliert zusammenzufassen, ohne dass relevante Informationen verloren gehen. Alle Konzepte, Begriffe und Theorien müssen vollständig und verständlich erklärt werden, auch wenn im Text keine direkte Erklärung dafür vorhanden ist. Falls ein Konzept oder Begriff ohne Erklärung auftaucht, füge eine vollständige und verständliche Erklärung hinzu, einschließlich praktischer Beispiele, wenn dies hilft, das Verständnis zu vertiefen. Es dürfen keine Themen ausgelassen oder stark verkürzt werden, da die Zusammenfassung zum Lernen für eine Klausur verwendet werden soll und daher genauso vollständig und informativ sein muss wie der Originaltext. Die Antwort muss lang genug sein, um alle wichtigen Details zu behandeln, und eine klare Struktur im JSON-Format aufweisen: {"summary":[{"topic": "Themenname", "points": ["Relevante Erklärung 1", "Relevante Erklärung 2", ...]}]} Achte darauf, dass auch komplexe Themen vollständig und detailliert erklärt werden, ohne sie zu verallgemeinern oder zu vereinfachen. Ergänze, wo möglich, praktische Beispiele, um das Verständnis der Konzepte zu fördern. Die Zusammenfassung muss alle Informationen enthalten, die für das Verständnis der Konzepte notwendig sind, und sollte als vollständige Lernressource dienen.""";
        private readonly IApiKeyProvider _apiKeyProvider;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiSummarizerService> _logger;
        #endregion

        public override string ModelName => "gemini-2.5-flash";

        public override Uri Endpoint => new("https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent");

        #region constructor
        public GeminiSummarizerService(IApiKeyProvider apiKeyProvider, ILogger<GeminiSummarizerService> logger)
        {
            this._apiKeyProvider = apiKeyProvider;
            this._logger = logger;
            this._httpClient = new HttpClient();
            this._httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKeyProvider.ApiKey);
        }
        #endregion
        #region public methods
        public async override Task<SummaryResponse> SummarizeText(string text)
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

            return summaries.MergeSummaries();
        }
        #endregion

        #region private methods
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

        private async Task<GeminiRequest> SendSummaryRequestToAPI(string textChunk, string[] alreadySummarizedTopics)
        {
            string text = UpdateUserMessage(textChunk, alreadySummarizedTopics);
            GeminiRequest requestBody = new GeminiRequest(text, this._instruction);
            
            return ParseResponse<GeminiRequest>(await SendPostRequestAsync(this.Endpoint, requestBody));
        }

        private static T ParseResponse<T>(string responseString) where T : class
        {
            var sanitizedResponse = responseString.Replace("\n", "").Replace("\r", "");
            var jsonResponse = JsonSerializer.Deserialize<T>(sanitizedResponse);

            if(jsonResponse == null)
            {
                throw new Exception("Failed to parse response from Gemini API.");
            }
            return jsonResponse;
        }

        private async Task<string> SendPostRequestAsync(Uri url, GeminiRequest requestBody)
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

        private string UpdateUserMessage(string textToSummarize, string[] alreadySummarizesTopics)
        {
            // The message we send will not only include the textToSummarize but also the already summarized topics
            // This is to make the AI know which topics are already summarized and which are not
            string alreadySummarizedTopicsText = string.Join(" ", alreadySummarizesTopics);
            string messageContent = $"Ich habe bereits folgende Themen zusammengefasst, falls du inhalt findest, welcher sich logisch einen der themen anschließt, dann fasse ihn unter dem gleichen Titel zusammen. {alreadySummarizedTopicsText}";

            // If there are already summarized topics, we need to include them in the message otherwise we just send the text to summarize
            string messageToSend = alreadySummarizesTopics.Length != 0 ? $"{messageContent} {textToSummarize}" : textToSummarize;

            return messageToSend;
        }

        #endregion
    }
}
