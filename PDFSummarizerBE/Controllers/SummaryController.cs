using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;
using PDFSummarizerBE.Services;

namespace PDFSummarizerBE.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SummaryController : ControllerBase
    {
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

                OpenAiApi aiAgent = new(apiKey);
                SummaryResponse result = await aiAgent.SummarizeText(extractedTextPerPage[0]);

                if (result is null)
                {
                    return StatusCode(500, new { message = "Error creating summary" });
                }

                return Ok(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating summary", error = ex.Message });
            }
        }
        
        private List<string> ExtractText(List<IFormFile> files)
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
