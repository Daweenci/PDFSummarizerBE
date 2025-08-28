using SumarizerService.Models;
using SumarizerService.Models.OpenAIResponse;

namespace SumarizerService.Core
{
    public abstract class SummarizerService : ISummarizerService
    {
        public abstract string ModelName { get; }
        public abstract Uri Endpoint { get; }

        public abstract Task<SummaryResponse> SummarizeText(string text);

        private static readonly int _tokenCharacterLength = 3;
        protected static int EstimateTokens(string word) => word.Length / _tokenCharacterLength;
    }
}
