using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Net;

namespace product_catalog_app.src.filters
{
    /// <summary>
    /// Global exception handling filter for consistent error responses
    /// </summary>
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;
            
            // Log the exception with full details
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

            // Determine the appropriate response based on exception type
            var (statusCode, message) = GetErrorResponse(exception);

            // Create standardized error response
            var errorResponse = new ErrorResponse
            {
                Message = message,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow
            };

            // Add stack trace only in development
            if (_environment.IsDevelopment())
            {
                errorResponse.Details = exception.ToString();
            }

            // Set the result based on request type
            if (IsApiRequest(context.HttpContext.Request))
            {
                // Return JSON for API requests
                context.Result = new ObjectResult(errorResponse)
                {
                    StatusCode = statusCode
                };
            }
            else
            {
                // Redirect to error page for web requests
                context.Result = new RedirectToActionResult("Error", "Home", new { statusCode });
            }

            context.ExceptionHandled = true;
        }

        private static (int statusCode, string message) GetErrorResponse(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => (400, "Invalid request data"),
                ArgumentException => (400, "Invalid arguments provided"),
                UnauthorizedAccessException => (401, "Unauthorized access"),
                FileNotFoundException => (404, "Resource not found"),
                InvalidOperationException => (409, "Operation not allowed"),
                TimeoutException => (408, "Request timeout"),
                _ => (500, "An unexpected error occurred")
            };
        }

        private static bool IsApiRequest(HttpRequest request)
        {
            return request.Headers.Accept.Any(h => h.Contains("application/json")) ||
                   request.Path.StartsWithSegments("/api");
        }
    }

    /// <summary>
    /// Standardized error response model
    /// </summary>
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
    }
}
