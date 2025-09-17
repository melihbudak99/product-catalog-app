using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using product_catalog_app.src.data;
using System.Reflection;

namespace product_catalog_app.src.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(ProductDbContext context, ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetHealth()
        {
            try
            {
                var startTime = DateTime.UtcNow;

                // Check database connectivity
                var canConnectToDb = await CanConnectToDatabase();
                
                // Get basic stats
                var productCount = await _context.Products.CountAsync();
                var categoryCount = await _context.Categories.CountAsync();
                
                var endTime = DateTime.UtcNow;
                var responseTime = (endTime - startTime).TotalMilliseconds;

                var healthStatus = new
                {
                    Status = canConnectToDb ? "Healthy" : "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    Database = new
                    {
                        Connected = canConnectToDb,
                        ProductCount = productCount,
                        CategoryCount = categoryCount
                    },
                    Performance = new
                    {
                        ResponseTimeMs = Math.Round(responseTime, 2),
                        Status = responseTime < 1000 ? "Good" : responseTime < 3000 ? "Slow" : "Critical"
                    }
                };

                if (canConnectToDb)
                {
                    return Ok(healthStatus);
                }
                else
                {
                    return StatusCode(503, healthStatus); // Service Unavailable
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(503, new
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Error = "Health check failed",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("ping")]
        public ActionResult<object> Ping()
        {
            return Ok(new
            {
                Status = "Alive",
                Timestamp = DateTime.UtcNow,
                Message = "Service is responding"
            });
        }

        private async Task<bool> CanConnectToDatabase()
        {
            try
            {
                await _context.Database.CanConnectAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection failed during health check");
                return false;
            }
        }
    }
}
