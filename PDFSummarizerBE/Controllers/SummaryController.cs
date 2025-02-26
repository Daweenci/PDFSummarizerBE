using System.Text;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;

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
                // Get the Downloads folder path
                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                string pdfTextFolderPath = Path.Combine(downloadsPath, "pdf_texts");

                // Ensure the "pdf_texts" folder exists
                if (!Directory.Exists(pdfTextFolderPath))
                {
                    Directory.CreateDirectory(pdfTextFolderPath);
                }

                List<string> extractedTextPerPage = ExtractText(files); 
                
                return Ok(new { message = "Text extracted and saved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error extracting text", error = ex.Message });
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
