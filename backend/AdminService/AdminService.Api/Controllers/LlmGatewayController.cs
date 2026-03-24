using AdminService.Application.UseCases.LlmGateway;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Api.Controllers;

/// <summary>
/// LLM Gateway admin endpoints — cost tracking, model distribution, provider health, and config.
/// </summary>
[ApiController]
[Route("api/admin/llm-gateway")]
[Produces("application/json")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class LlmGatewayController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LlmGatewayController> _logger;

    public LlmGatewayController(IMediator mediator, ILogger<LlmGatewayController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get LLM API cost breakdown for the current (or specified) month.
    /// </summary>
    /// <param name="period">Optional period in YYYY-MM format. Defaults to current month.</param>
    [HttpGet("cost")]
    [ProducesResponseType(typeof(CostBreakdownDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CostBreakdownDto>> GetCost([FromQuery] string? period = null)
    {
        _logger.LogInformation("Admin requested LLM cost breakdown, period={Period}", period);
        var result = await _mediator.Send(new GetLlmCostQuery(period));
        return Ok(result);
    }

    /// <summary>
    /// Get model usage distribution (Claude vs Gemini vs Llama vs Cache).
    /// </summary>
    [HttpGet("distribution")]
    [ProducesResponseType(typeof(ModelDistributionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ModelDistributionDto>> GetDistribution()
    {
        _logger.LogInformation("Admin requested model distribution stats");
        var result = await _mediator.Send(new GetModelDistributionQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get health status of all configured LLM providers.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ProviderHealthDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProviderHealthDto>> GetHealth()
    {
        _logger.LogInformation("Admin requested LLM provider health");
        var result = await _mediator.Send(new GetProviderHealthQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get current LLM gateway configuration.
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(GatewayConfigDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GatewayConfigDto>> GetConfig()
    {
        _logger.LogInformation("Admin requested gateway config");
        var result = await _mediator.Send(new GetGatewayConfigQuery());
        return Ok(result);
    }

    /// <summary>
    /// Enable or disable aggressive cache mode to maximize prompt cache savings.
    /// </summary>
    [HttpPost("cost/aggressive-mode")]
    [ProducesResponseType(typeof(AggressiveModeResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AggressiveModeResponseDto>> ToggleAggressiveMode([FromBody] ToggleAggressiveModeRequest request)
    {
        _logger.LogInformation("Admin toggling aggressive cache mode to {Enable}", request.Enable);
        var result = await _mediator.Send(new ToggleAggressiveModeCommand(request.Enable));
        return Ok(result);
    }
}

public sealed class ToggleAggressiveModeRequest
{
    public bool Enable { get; set; }
}
