using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using SumarizerService.Models;
using SumarizerService.Models.OpenAIResponse;
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
        var html = htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var dictionary = new Dictionary<string, object?>
            {
                { "response", response }
            };
            var parameters = ParameterView.FromDictionary(dictionary);
            var output = await htmlRenderer.RenderComponentAsync<PDFView>(parameters);
            return output.ToHtmlString();
        });

        using var playwright = await Playwright.CreateAsync();

        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false // Changed to false to allow UI interaction
        });

        string downloadPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        var contextOptions = new BrowserNewContextOptions
        {
            AcceptDownloads = true // Make sure the browser accepts downloads
        };

        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();

        var pdfPath = Path.Combine(downloadPath, "summary.pdf");

        // Generate the PDF
        await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            Path = pdfPath  // Path where the PDF will be saved
        });

        _logger.LogDebug("PDF downloaded at: {pdfPath}", pdfPath);
    }
}
