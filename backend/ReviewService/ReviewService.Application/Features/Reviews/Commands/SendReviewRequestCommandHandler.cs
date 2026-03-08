using MediatR;
using Microsoft.Extensions.Logging;
using ReviewService.Domain.Base;
using ReviewService.Domain.Entities;
using ReviewService.Domain.Interfaces;

namespace ReviewService.Application.Features.Reviews.Commands;

/// <summary>
/// Handler para enviar solicitud de review
/// </summary>
public class SendReviewRequestCommandHandler : IRequestHandler<SendReviewRequestCommand, Result<ReviewRequestResultDto>>
{
    private readonly IReviewRequestRepository _requestRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly ILogger<SendReviewRequestCommandHandler> _logger;

    public SendReviewRequestCommandHandler(
        IReviewRequestRepository requestRepository, 
        IReviewRepository reviewRepository,
        ILogger<SendReviewRequestCommandHandler> logger)
    {
        _requestRepository = requestRepository;
        _reviewRepository = reviewRepository;
        _logger = logger;
    }

    public async Task<Result<ReviewRequestResultDto>> Handle(SendReviewRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verificar si ya existe una solicitud para esta orden
            var existingRequest = await _requestRepository.ExistsForOrderAsync(request.OrderId, cancellationToken);

            if (existingRequest)
            {
                return Result<ReviewRequestResultDto>.Failure("Ya existe una solicitud de review para esta orden");
            }

            // Verificar si ya existe una review para esta orden
            var existingReview = await _reviewRepository.GetByOrderIdAsync(request.OrderId);

            if (existingReview != null)
            {
                return Result<ReviewRequestResultDto>.Failure("Ya existe una review para esta orden");
            }

            // Generar token único para el link
            var token = GenerateSecureToken();

            // Crear solicitud de review
            var reviewRequest = new ReviewRequest
            {
                Id = Guid.NewGuid(),
                BuyerId = request.BuyerId,
                SellerId = request.SellerId,
                VehicleId = request.VehicleId,
                OrderId = request.OrderId,
                BuyerEmail = request.BuyerEmail,
                BuyerName = request.BuyerName,
                PurchaseDate = request.PurchaseDate,
                RequestSentAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30), // 30 días para escribir la review
                Status = ReviewRequestStatus.Sent,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _requestRepository.AddAsync(reviewRequest);

            _logger.LogInformation(
                "Review request sent for order {OrderId}. RequestId: {RequestId}", 
                request.OrderId, reviewRequest.Id);

            // TODO: Enviar email al comprador via NotificationService
            // await _notificationService.SendReviewRequestEmail(reviewRequest);

            return Result<ReviewRequestResultDto>.Success(new ReviewRequestResultDto
            {
                RequestId = reviewRequest.Id,
                Token = token,
                ExpiresAt = reviewRequest.ExpiresAt,
                EmailSent = true, // Simulated
                Message = $"Solicitud de review enviada a {request.BuyerEmail}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending review request for order {OrderId}", request.OrderId);
            return Result<ReviewRequestResultDto>.Failure($"Error al enviar solicitud: {ex.Message}");
        }
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
