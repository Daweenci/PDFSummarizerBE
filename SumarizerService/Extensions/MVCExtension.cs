using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SumarizerService.Core;
using SumarizerService.Middleware;

namespace SumarizerService.Extensions
{
    public static class MVCExtension
    {
        public static IServiceCollection AddSummarizerService(this IServiceCollection services)
        {
            services.AddSingleton<IApiKeyProvider, ApiKeyProvider>();
            services.AddSingleton<ISummarizerService, GeminiSummarizerService>();
            return services;
        }

        public static IApplicationBuilder UseApiKeyMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ApiKeyMiddleware>();
            return app;
        }
    }
}
