using CRMService.Application.DTOs;
using CRMService.Domain.Entities;
using CRMService.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMService.Api.Controllers;

[ApiController]
[Route("api/crm/[controller]")]
[Authorize]
public class PipelinesController : ControllerBase
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IDealRepository _dealRepository;
    private readonly ILogger<PipelinesController> _logger;

    public PipelinesController(
        IPipelineRepository pipelineRepository,
        IDealRepository dealRepository,
        ILogger<PipelinesController> logger)
    {
        _pipelineRepository = pipelineRepository;
        _dealRepository = dealRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PipelineDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var pipelines = await _pipelineRepository.GetAllAsync(cancellationToken);
        return Ok(pipelines.Select(PipelineDto.FromEntity));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PipelineDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var pipeline = await _pipelineRepository.GetByIdWithStagesAsync(id, cancellationToken);
        if (pipeline == null)
            return NotFound();

        return Ok(PipelineDto.FromEntity(pipeline));
    }

    [HttpGet("default")]
    public async Task<ActionResult<PipelineDto>> GetDefault(CancellationToken cancellationToken = default)
    {
        var pipeline = await _pipelineRepository.GetDefaultAsync(cancellationToken);
        if (pipeline == null)
            return NotFound("No default pipeline configured");

        return Ok(PipelineDto.FromEntity(pipeline));
    }

    [HttpPost]
    public async Task<ActionResult<PipelineDto>> Create(
        [FromBody] CreatePipelineRequest request,
        [FromHeader(Name = "X-Dealer-Id")] Guid dealerId,
        CancellationToken cancellationToken = default)
    {
        var pipeline = new Pipeline(dealerId, request.Name, request.Description, request.IsDefault);

        // Add stages if provided
        if (request.Stages != null)
        {
            foreach (var stageRequest in request.Stages.OrderBy(s => s.Order))
            {
                pipeline.AddStage(stageRequest.Name, stageRequest.Order, stageRequest.Color);
            }
        }

        await _pipelineRepository.AddAsync(pipeline, cancellationToken);

        _logger.LogInformation("Pipeline {PipelineId} created for dealer {DealerId}", pipeline.Id, dealerId);

        return CreatedAtAction(nameof(GetById), new { id = pipeline.Id }, PipelineDto.FromEntity(pipeline));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PipelineDto>> Update(
        Guid id,
        [FromBody] UpdatePipelineRequest request,
        CancellationToken cancellationToken = default)
    {
        var pipeline = await _pipelineRepository.GetByIdAsync(id, cancellationToken);
        if (pipeline == null)
            return NotFound();

        pipeline.Update(request.Name, request.Description);

        if (request.IsDefault)
        {
            pipeline.SetAsDefault();
        }

        await _pipelineRepository.UpdateAsync(pipeline, cancellationToken);

        _logger.LogInformation("Pipeline {PipelineId} updated", id);

        return Ok(PipelineDto.FromEntity(pipeline));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _pipelineRepository.ExistsAsync(id, cancellationToken))
            return NotFound();

        await _pipelineRepository.DeleteAsync(id, cancellationToken);

        _logger.LogInformation("Pipeline {PipelineId} deleted", id);

        return NoContent();
    }

    /// <summary>
    /// Get all deals for a pipeline
    /// </summary>
    [HttpGet("{id:guid}/deals")]
    public async Task<ActionResult<IEnumerable<DealDto>>> GetDeals(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _pipelineRepository.ExistsAsync(id, cancellationToken))
            return NotFound("Pipeline not found");

        var deals = await _dealRepository.GetByPipelineAsync(id, cancellationToken);
        return Ok(deals.Select(DealDto.FromEntity));
    }

    /// <summary>
    /// Get pipeline stage statistics (for Kanban board)
    /// </summary>
    [HttpGet("{id:guid}/stats")]
    public async Task<ActionResult<IEnumerable<PipelineStageStatsDto>>> GetStats(Guid id, CancellationToken cancellationToken = default)
    {
        var pipeline = await _pipelineRepository.GetByIdWithStagesAsync(id, cancellationToken);
        if (pipeline == null)
            return NotFound("Pipeline not found");

        var deals = await _dealRepository.GetByPipelineAsync(id, cancellationToken);
        var dealDtos = deals.Select(DealDto.FromEntity).ToList();

        var stats = pipeline.Stages.OrderBy(s => s.Order).Select(stage =>
        {
            var stageDeals = dealDtos.Where(d => d.StageId == stage.Id).ToList();
            return new PipelineStageStatsDto
            {
                StageId = stage.Id,
                StageName = stage.Name,
                Color = stage.Color ?? "#6366F1",
                Deals = stageDeals,
                TotalValue = stageDeals.Sum(d => d.Value),
                Count = stageDeals.Count
            };
        }).ToList();

        return Ok(stats);
    }
}
