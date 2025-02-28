using Microsoft.AspNetCore.Mvc;
using SumarizerService;
using SumarizerService.Models.OpenAIResponse;
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
        private readonly ISummarizerService _openAISummarizerService;
        private readonly PDFService.PDFService _pdfService;

        public SummaryController(ILogger<SummaryController> logger, ISummarizerService openAISummarizerService, PDFService.PDFService pdfService)
        {
            _logger = logger;
            _openAISummarizerService = openAISummarizerService;
            _pdfService = pdfService;
        }

        [HttpPost]
        public async Task<IActionResult> ReceivePdfs([FromHeader] string apiKey, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "No files received" });
            }

            try
            {
                List<string> extractedTextPerPage = ExtractText(files);

                SummaryResponse result = await _openAISummarizerService.SummarizeText(extractedTextPerPage[0], apiKey);

                if (result is null)
                {
                    return StatusCode(500, new { message = "Error creating summary" });
                }

                await _pdfService.InitializeAsync(result);

                return Ok(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating summary", error = ex.Message });
            }
        }

        private static List<string> ExtractText(List<IFormFile> files)
        {
            List<string> fileTexts = [];

            // Process each PDF file
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
