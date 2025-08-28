using SumarizerService.Extensions;

namespace PDFSummarizerBE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSummarizerService();

            // TODO: Move this to Extension Method
            builder.Services.AddSingleton<PDFService.PDFService>();
            // Do the Logger Factory 
            builder.Services.AddLogging();

            // Add CORS policy to allow requests from your Chrome extension
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowChromeExtension", policy =>
                {
                    policy.WithOrigins("chrome-extension://meedgfhcijenfiijaecfiomilomkocdl")
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseApiKeyMiddleware();

            app.UseCors("AllowChromeExtension");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
