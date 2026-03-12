using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserService.Application.Interfaces;

/// <summary>
/// Cliente HTTP para comunicarse con ReviewService
/// </summary>
public interface IReviewServiceClient
{
    /// <summary>
    /// Obtiene las reseñas de un vendedor
    /// </summary>
    Task<SellerReviewsResult> GetSellerReviewsAsync(
        Guid sellerId,
        int page = 1,
        int pageSize = 10,
        int? rating = null);

    /// <summary>
    /// Obtiene el resumen de ratings de un vendedor
    /// </summary>
    Task<SellerRatingSummary?> GetSellerRatingSummaryAsync(Guid sellerId);
}

/// <summary>
/// Resultado de reseñas del vendedor desde ReviewService
/// </summary>
public class SellerReviewsResult
{
    public List<SellerReviewItemDto> Reviews { get; set; } = new();
    public double AverageRating { get; set; }
    public int TotalCount { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new()
    {
        { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 }
    };
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// DTO de reseña individual
/// </summary>
public class SellerReviewItemDto
{
    public Guid Id { get; set; }
    public Guid ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string? ReviewerPhotoUrl { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? VehicleTitle { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public ReviewReplyDto? Reply { get; set; }
}

/// <summary>
/// DTO de respuesta a reseña
/// </summary>
public class ReviewReplyDto
{
    public string Content { get; set; } = string.Empty;
    public DateTime RepliedAt { get; set; }
}

/// <summary>
/// Resumen de ratings del vendedor
/// </summary>
public class SellerRatingSummary
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> Distribution { get; set; } = new();
    public int ResponseRate { get; set; }
    public int AverageResponseTimeMinutes { get; set; }
}
