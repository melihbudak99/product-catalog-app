using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace product_catalog_app.src.controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            _logger.LogWarning("HTTP {StatusCode} error occurred. Request path: {Path}", 
                statusCode, HttpContext.Request.Path);

            return statusCode switch
            {
                404 => View("Error404"),
                500 => View("Error500"),
                _ => View("Error", new { StatusCode = statusCode })
            };
        }

        [Route("Error")]
        public IActionResult Error()
        {
            _logger.LogError("Unhandled error occurred. Request path: {Path}", HttpContext.Request.Path);
            return View("Error500");
        }
    }
}
