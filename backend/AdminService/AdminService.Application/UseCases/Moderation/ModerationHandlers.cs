using AdminService.Application.DTOs;
using AdminService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdminService.Application.UseCases.Moderation;

/// <summary>
/// Handler for GetModerationQueueQuery
/// </summary>
public class GetModerationQueueHandler : IRequestHandler<GetModerationQueueQuery, PaginatedModerationResponse>
{
    private readonly IModerationRepository _repository;
    private readonly ILogger<GetModerationQueueHandler> _logger;

    public GetModerationQueueHandler(IModerationRepository repository, ILogger<GetModerationQueueHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PaginatedModerationResponse> Handle(GetModerationQueueQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting moderation queue: Type={Type}, Priority={Priority}, Page={Page}",
            request.Type, request.Priority, request.Page);

        var (items, total) = await _repository.GetModerationQueueAsync(
            request.Type, request.Priority, request.Status,
            request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(item => new ModerationItemDto(
            item.Id,
            item.Type,
            item.TargetId,
            item.Title,
            item.Description,
            item.Price,
            item.Images,
            item.SellerName,
            item.SellerType,
            item.SellerId.ToString(),
            item.SubmittedAt,
            item.FlagReason,
            item.Priority,
            item.Status,
            item.ReviewedAt,
            item.ReviewedById,
            item.ReviewerNotes,
            item.RejectionReason
        )).ToList();

        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)total / request.PageSize) : 0;

        return new PaginatedModerationResponse(dtos, total, request.Page, request.PageSize, totalPages);
    }
}

/// <summary>
/// Handler for GetModerationItemQuery
/// </summary>
public class GetModerationItemHandler : IRequestHandler<GetModerationItemQuery, ModerationItemDto?>
{
    private readonly IModerationRepository _repository;
    private readonly ILogger<GetModerationItemHandler> _logger;

    public GetModerationItemHandler(IModerationRepository repository, ILogger<GetModerationItemHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ModerationItemDto?> Handle(GetModerationItemQuery request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (item is null)
        {
            _logger.LogWarning("Moderation item not found: {Id}", request.Id);
            return null;
        }

        return new ModerationItemDto(
            item.Id,
            item.Type,
            item.TargetId,
            item.Title,
            item.Description,
            item.Price,
            item.Images,
            item.SellerName,
            item.SellerType,
            item.SellerId.ToString(),
            item.SubmittedAt,
            item.FlagReason,
            item.Priority,
            item.Status,
            item.ReviewedAt,
            item.ReviewedById,
            item.ReviewerNotes,
            item.RejectionReason
        );
    }
}

/// <summary>
/// Handler for GetModerationStatsQuery
/// </summary>
public class GetModerationStatsHandler : IRequestHandler<GetModerationStatsQuery, ModerationStatsDto>
{
    private readonly IModerationRepository _repository;
    private readonly ILogger<GetModerationStatsHandler> _logger;

    public GetModerationStatsHandler(IModerationRepository repository, ILogger<GetModerationStatsHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ModerationStatsDto> Handle(GetModerationStatsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting moderation statistics");

        var stats = await _repository.GetStatsAsync(cancellationToken);

        return new ModerationStatsDto(
            stats.InQueue,
            stats.HighPriority,
            stats.ReviewedToday,
            stats.RejectedToday,
            stats.ApprovedToday,
            stats.EscalatedCount,
            stats.AvgReviewTimeMinutes
        );
    }
}

/// <summary>
/// Handler for ProcessModerationActionCommand
/// </summary>
public class ProcessModerationActionHandler : IRequestHandler<ProcessModerationActionCommand, ModerationActionResponseDto>
{
    private readonly IModerationRepository _repository;
    private readonly ILogger<ProcessModerationActionHandler> _logger;

    public ProcessModerationActionHandler(IModerationRepository repository, ILogger<ProcessModerationActionHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ModerationActionResponseDto> Handle(ProcessModerationActionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing moderation action: ItemId={ItemId}, Action={Action}, ReviewerId={ReviewerId}",
            request.ItemId, request.Action, request.ReviewerId);

        // Validate action
        var validActions = new[] { "approve", "reject", "escalate", "skip" };
        if (!validActions.Contains(request.Action.ToLowerInvariant()))
        {
            return new ModerationActionResponseDto(false, $"Invalid action: {request.Action}. Valid actions are: {string.Join(", ", validActions)}", null);
        }

        var success = await _repository.ProcessActionAsync(
            request.ItemId,
            request.Action,
            request.ReviewerId,
            request.Reason,
            request.Notes,
            cancellationToken);

        if (!success)
        {
            return new ModerationActionResponseDto(false, "Item not found or action could not be processed", null);
        }

        var message = request.Action.ToLowerInvariant() switch
        {
            "approve" => "Item approved successfully",
            "reject" => "Item rejected",
            "escalate" => "Item escalated for further review",
            "skip" => "Item skipped, will appear later in queue",
            _ => "Action processed"
        };

        // Get next item in queue
        var (nextItems, _) = await _repository.GetModerationQueueAsync(
            status: "pending", page: 1, pageSize: 1, cancellationToken: cancellationToken);
        var nextItemId = nextItems.FirstOrDefault()?.Id;

        return new ModerationActionResponseDto(true, message, nextItemId);
    }
}
