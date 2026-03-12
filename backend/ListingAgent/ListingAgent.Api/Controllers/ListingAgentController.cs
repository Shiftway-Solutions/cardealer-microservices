using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ListingAgent.Application.DTOs;
using ListingAgent.Application.Features.Listing.Queries;
using ListingAgent.Domain.Interfaces;

namespace ListingAgent.Api.Controllers;

[ApiController]
[Route("api/listing-agent")]
public sealed class ListingAgentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IListingCacheMetrics _cacheMetrics;

    public ListingAgentController(IMediator mediator, IListingCacheMetrics cacheMetrics)
    {
        _mediator = mediator;
        _cacheMetrics = cacheMetrics;
    }

    /// <summary>
    /// Optimize a vehicle listing: generate SEO title, description, quality score, and tips.
    /// </summary>
    [HttpPost("optimize")]
    [Authorize]
    public async Task<ActionResult<ListingOptimizationResponse>> OptimizeListing(
        [FromBody] ListingOptimizationRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new OptimizeListingQuery(request), ct);
        return Ok(result);
    }

    /// <summary>
    /// Redis cache hit rate metrics for the ListingAgent.
    /// Reports total requests, hits, misses, and whether the ≥50% target is met.
    /// </summary>
    [HttpGet("metrics/cache")]
    [Authorize]
    public async Task<IActionResult> GetCacheMetrics(CancellationToken ct)
    {
        var stats = await _cacheMetrics.GetStatsAsync(ct);
        return Ok(new
        {
            totalRequests = stats.TotalRequests,
            cacheHits = stats.CacheHits,
            cacheMisses = stats.CacheMisses,
            hitRatePercent = stats.HitRatePercent,
            targetMet = stats.TargetMet,
            targetPercent = 50.0,
            measuredAt = stats.MeasuredAt,
            status = stats.TargetMet ? "✅ target_met" : "⚠️ below_target"
        });
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "healthy", agent = "ListingAgent", version = "1.0.0" });
}
