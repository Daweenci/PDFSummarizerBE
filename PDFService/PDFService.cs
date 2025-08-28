using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using SumarizerService.Models.OpenAIResponse;
using System.Diagnostics;

namespace PDFService;

public class PDFService
{
    private readonly ILogger<PDFService> _logger;

    public PDFService(ILogger<PDFService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(SummaryResponse response)
    {
        _logger.LogInformation("Creating PDF from summary");
        Microsoft.Playwright.Program.Main(["install"]);

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        await using var htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);

        var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var dictionary = new Dictionary<string, object?>
            {
                { "response", response }
            };
            var parameters = ParameterView.FromDictionary(dictionary);
            var output = await htmlRenderer.RenderComponentAsync<PDFView>(parameters);
            return output.ToHtmlString();
        });

        // Save HTML to a file
        string downloadPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        string htmlFilePath = Path.Combine(downloadPath, "summary.html");

        await File.WriteAllTextAsync(htmlFilePath, html);

        _logger.LogInformation("HTML file created at: {path}", htmlFilePath);

        using var playwright = await Playwright.CreateAsync();

        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false // Open a real browser window
        });

        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        // Open the local HTML file
        await page.GotoAsync(new Uri(htmlFilePath).AbsoluteUri);

        var pdfPath = Path.Combine(downloadPath, "summary.pdf");

        // Generate the PDF
        await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            Path = pdfPath  // Path where the PDF will be saved
        });

        _logger.LogInformation("Browser opened and displaying the HTML file.");
    }
}
