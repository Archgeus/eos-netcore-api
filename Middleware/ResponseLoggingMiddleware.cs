using System.Text;

namespace EOS.Middleware;

public class ResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseLoggingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ResponseLoggingMiddleware(RequestDelegate next, ILogger<ResponseLoggingMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            await _next(context);

            // Only log in Development mode (contains sensitive data)
            if (_env.IsDevelopment())
            {
                // Read response
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
                
                // Log response
                _logger.LogInformation($"Response: {context.Request.Method} {context.Request.Path} - Status: {context.Response.StatusCode}");
                if (!string.IsNullOrEmpty(body))
                {
                    _logger.LogInformation($"Response Body: {body}");
                }
            }

            // Always reset position before copying to client
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}
