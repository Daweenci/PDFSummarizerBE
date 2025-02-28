using Microsoft.AspNetCore.Http;

namespace SumarizerService.Middleware
{
    public class ApiKeyMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task Invoke(HttpContext context, IApiKeyProvider apiKeyProvider)
        {
            if (context.Request.Headers.TryGetValue("apiKey", out var apiKeyValue))
            {
                apiKeyProvider.ApiKey = apiKeyValue!;
            }

            await _next(context);
        }
    }

}
