using MediatR;
using Microsoft.Extensions.Logging;
using ReviewService.Domain.Base;
using ReviewService.Domain.Entities;
using ReviewService.Domain.Interfaces;

namespace ReviewService.Application.Features.Reviews.Queries;

/// <summary>
/// Sprint 15 - Obtener solicitudes de review pendientes para un comprador
/// </summary>
public record GetPendingReviewRequestsQuery : IRequest<Result<List<ReviewRequestDto>>>
{
    public Guid BuyerId { get; init; }
}

/// <summary>
/// DTO para solicitud de review
/// </summary>
public record ReviewRequestDto
{
    public Guid Id { get; init; }
    public Guid SellerId { get; init; }
    public Guid VehicleId { get; init; }
    public Guid OrderId { get; init; }
    public string SellerName { get; init; } = string.Empty;
    public string VehicleTitle { get; init; } = string.Empty;
    public DateTime PurchaseDate { get; init; }
    public DateTime RequestSentAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public bool IsExpired { get; init; }
    public int DaysRemaining { get; init; }
}

/// <summary>
/// Handler para obtener solicitudes de review pendientes
/// </summary>
public class GetPendingReviewRequestsQueryHandler : IRequestHandler<GetPendingReviewRequestsQuery, Result<List<ReviewRequestDto>>>
{
    private readonly IReviewRequestRepository _requestRepository;
    private readonly ILogger<GetPendingReviewRequestsQueryHandler> _logger;

    public GetPendingReviewRequestsQueryHandler(IReviewRequestRepository requestRepository, ILogger<GetPendingReviewRequestsQueryHandler> logger)
    {
        _requestRepository = requestRepository;
        _logger = logger;
    }

    public async Task<Result<List<ReviewRequestDto>>> Handle(GetPendingReviewRequestsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;

            var buyerRequests = await _requestRepository.GetByBuyerIdAsync(request.BuyerId, ReviewRequestStatus.Sent, cancellationToken);

            var pendingRequests = buyerRequests
                .Where(r => r.ExpiresAt > now && r.ReviewId == null)
                .OrderByDescending(r => r.RequestSentAt)
                .Select(r => new ReviewRequestDto
                {
                    Id = r.Id,
                    SellerId = r.SellerId,
                    VehicleId = r.VehicleId,
                    OrderId = r.OrderId,
                    SellerName = "", // Se obtendría del UserService
                    VehicleTitle = "", // Se obtendría del VehiclesSaleService
                    PurchaseDate = r.PurchaseDate,
                    RequestSentAt = r.RequestSentAt,
                    ExpiresAt = r.ExpiresAt,
                    Status = r.Status.ToString(),
                    Token = r.Token,
                    IsExpired = r.ExpiresAt <= now,
                    DaysRemaining = (int)Math.Max(0, (r.ExpiresAt - now).TotalDays)
                })
                .ToList();

            return Result<List<ReviewRequestDto>>.Success(pendingRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending review requests for buyer {BuyerId}", request.BuyerId);
            return Result<List<ReviewRequestDto>>.Failure($"Error al obtener solicitudes: {ex.Message}");
        }
    }
}
