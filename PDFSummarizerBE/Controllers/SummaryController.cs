using Microsoft.AspNetCore.Mvc;
using SumarizerService;
using SumarizerService.Models;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace PDFSummarizerBE.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SummaryController : ControllerBase
    {
        private readonly ILogger<SummaryController> _logger;
        private readonly ISummarizerService _summarizerService;
        private readonly PDFService.PDFService _pdfService;

        public SummaryController(ILogger<SummaryController> logger, ISummarizerService openAISummarizerService, PDFService.PDFService pdfService)
        {
            _logger = logger;
            _summarizerService = openAISummarizerService;
            _pdfService = pdfService;
        }

        [HttpPost]
        public async Task<IActionResult> ReceivePdfs(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "No files received" });
            }

            try
            {
                List<string> extractedTextPerFile = ExtractText(files);

                SummaryResponse[] results = await SummarizeFilesInOrder(extractedTextPerFile);

                if (results is null || results.Length == 0)
                {
                    return StatusCode(500, new { message = "Error creating summary" });
                }

                await _pdfService.InitializeAsync(results[0]);

                return Ok(JsonSerializer.Serialize(results));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating summary", error = ex.Message });
            }
        }

        private async Task<SummaryResponse[]> SummarizeFilesInOrder(List<string> extractedTextPerFile)
        {
            var tasks = extractedTextPerFile
                .Select((text, index) => SummarizeWithIndex(text, index))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            return results.OrderBy(r => r.Index).Select(r => r.Response).ToArray();
        }

        private async Task<(int Index, SummaryResponse Response)> SummarizeWithIndex(string text, int index)
        {
            var response = await _summarizerService.SummarizeText(text);
            return (index, response);
        }

        private static List<string> ExtractText(List<IFormFile> files)
        {
            List<string> fileTexts = [];

            foreach (var file in files)
            {
                StringBuilder sb = new();
                if (file.Length > 0)
                {
                    using var stream = file.OpenReadStream();
                    using var pdf = PdfDocument.Open(stream);

                    // Extract text from each page
                    foreach (var page in pdf.GetPages())
                    {
                        sb.Append(page.Text);
                        sb.AppendLine("\n");
                    }
                }
                fileTexts.Add(sb.ToString());
            }

            return fileTexts;
        }
    }
}
