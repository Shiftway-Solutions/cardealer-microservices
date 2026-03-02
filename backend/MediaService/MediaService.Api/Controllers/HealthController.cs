// backend\MediaService\MediaService.Api\Controllers\HealthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MediaService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Exclude 'external' tagged checks (DB, storage) per project standards:
        // only lightweight checks run on /health (liveness)
        var report = await _healthCheckService.CheckHealthAsync(
            check => !check.Tags.Contains("external"));
        return report.Status == HealthStatus.Healthy ? Ok(report) : StatusCode(503, report);
    }
}