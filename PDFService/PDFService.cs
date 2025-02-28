using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using SumarizerService.Models.OpenAIResponse;
namespace PDFService;

public class PDFService
{
    public PDFService(SummaryResponse response)
    {
        InitializeAsync(response).GetAwaiter().GetResult();
    }

    private async Task InitializeAsync(SummaryResponse response)
    {
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
            Headless = true
        });

        string downloadPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        var contextOptions = new BrowserNewContextOptions
        {
            AcceptDownloads = true // Make sure the browser accepts downloads
        };

        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();

        await page.SetContentAsync(await html);

        var pdfPath = Path.Combine(downloadPath, "summary.pdf");

        await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            Path = pdfPath  // Path where the PDF will be saved
        });

        await page.CloseAsync();
        await browser.CloseAsync();

        Console.WriteLine($"PDF downloaded at: {pdfPath}");
    }
}
