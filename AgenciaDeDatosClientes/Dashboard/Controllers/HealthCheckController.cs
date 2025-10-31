using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Controllers
{
    public class HealthCheckController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(CacheProfileName = "NoCache")]
        [Route("healthcheck")]
        public IActionResult Get()
        {
            var response = new
            {
                Status = "OK",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                UtcDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                UtcTime = DateTime.UtcNow.ToString("HH:mm:ss.fff")
            };

            return Ok(response);
        }
    }
} 