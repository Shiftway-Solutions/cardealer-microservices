using MediatR;
using ReviewService.Application.DTOs;
using ReviewService.Domain.Base;

namespace ReviewService.Application.Features.Reviews.Commands;

/// <summary>
/// Command para crear una nueva review
/// </summary>
public record CreateReviewCommand : IRequest<Result<ReviewDto>>
{
    public Guid BuyerId { get; init; }
    public Guid SellerId { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? OrderId { get; init; }
    public int Rating { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;

    // Info del buyer para cache
    public string BuyerName { get; init; } = string.Empty;
    public string? BuyerPhotoUrl { get; init; }
}