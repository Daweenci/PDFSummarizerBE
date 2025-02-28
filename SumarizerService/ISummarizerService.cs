using SumarizerService.Models.OpenAIResponse;

namespace SumarizerService
{
    public interface ISummarizerService
    {
        public Task<SummaryResponse> SummarizeText(string text, string apiKey);
    }
}
