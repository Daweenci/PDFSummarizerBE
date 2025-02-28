namespace SumarizerService.Middleware
{
    public class ApiKeyProvider : IApiKeyProvider
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}
