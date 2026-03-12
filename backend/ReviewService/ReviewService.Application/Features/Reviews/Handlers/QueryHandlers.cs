using MediatR;
using ReviewService.Application.Features.Reviews.Queries;
using ReviewService.Application.DTOs;
using ReviewService.Domain.Interfaces;
using ReviewService.Domain.Base;

namespace ReviewService.Application.Features.Reviews.Handlers;

/// <summary>
/// Handler para GetSellerReviewsQuery
/// </summary>
public class GetSellerReviewsQueryHandler : IRequestHandler<GetSellerReviewsQuery, Result<PagedReviewsDto>>
{
    private readonly IReviewRepository _reviewRepository;

    public GetSellerReviewsQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<Result<PagedReviewsDto>> Handle(GetSellerReviewsQuery request, CancellationToken cancellationToken)
    {
        // Obtener reviews paginadas
        var (reviews, totalCount) = await _reviewRepository.GetBySellerIdAsync(
            request.SellerId,
            request.Page,
            request.PageSize,
            request.OnlyApproved);

        // Convertir a DTOs
        var reviewDtos = reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            BuyerId = r.BuyerId,
            SellerId = r.SellerId,
            VehicleId = r.VehicleId,
            OrderId = r.OrderId,
            Rating = r.Rating,
            Title = r.Title,
            Content = r.Content,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            BuyerName = r.BuyerName,
            BuyerPhotoUrl = r.BuyerPhotoUrl,
            TrustScore = r.TrustScore,
            WasAutoRequested = r.WasAutoRequested,
            CreatedAt = r.CreatedAt,
            Response = r.Response != null ? new ReviewResponseDto
            {
                Id = r.Response.Id,
                ReviewId = r.Response.ReviewId,
                SellerId = r.Response.SellerId,
                Content = r.Response.Content,
                SellerName = r.Response.SellerName,
                CreatedAt = r.Response.CreatedAt
            } : null
        });

        // Calcular paginación
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var result = new PagedReviewsDto
        {
            Reviews = reviewDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNextPage = request.Page < totalPages,
            HasPreviousPage = request.Page > 1
        };

        return Result<PagedReviewsDto>.Success(result);
    }
}

/// <summary>
/// Handler para GetReviewSummaryQuery
/// </summary>
public class GetReviewSummaryQueryHandler : IRequestHandler<GetReviewSummaryQuery, Result<ReviewSummaryDto>>
{
    private readonly IReviewSummaryRepository _summaryRepository;

    public GetReviewSummaryQueryHandler(IReviewSummaryRepository summaryRepository)
    {
        _summaryRepository = summaryRepository;
    }

    public async Task<Result<ReviewSummaryDto>> Handle(GetReviewSummaryQuery request, CancellationToken cancellationToken)
    {
        // Obtener o crear summary
        var summary = await _summaryRepository.GetOrCreateBySellerIdAsync(request.SellerId);

        // Convertir a DTO
        var dto = new ReviewSummaryDto
        {
            SellerId = summary.SellerId,
            TotalReviews = summary.TotalReviews,
            AverageRating = summary.AverageRating,
            RatingDistribution = summary.GetRatingDistribution(),
            PositivePercentage = summary.PositivePercentage,
            VerifiedPurchaseReviews = summary.VerifiedPurchaseReviews,
            LastReviewDate = summary.LastReviewDate
        };

        return Result<ReviewSummaryDto>.Success(dto);
    }
}

/// <summary>
/// Handler para GetReviewByIdQuery
/// </summary>
public class GetReviewByIdQueryHandler : IRequestHandler<GetReviewByIdQuery, Result<ReviewDto>>
{
    private readonly IReviewRepository _reviewRepository;

    public GetReviewByIdQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<Result<ReviewDto>> Handle(GetReviewByIdQuery request, CancellationToken cancellationToken)
    {
        var review = await _reviewRepository.GetByIdAsync(request.ReviewId);

        if (review == null)
        {
            return Result<ReviewDto>.Failure("Review no encontrada");
        }

        var dto = new ReviewDto
        {
            Id = review.Id,
            BuyerId = review.BuyerId,
            SellerId = review.SellerId,
            VehicleId = review.VehicleId,
            OrderId = review.OrderId,
            Rating = review.Rating,
            Title = review.Title,
            Content = review.Content,
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            BuyerName = review.BuyerName,
            BuyerPhotoUrl = review.BuyerPhotoUrl,
            TrustScore = review.TrustScore,
            WasAutoRequested = review.WasAutoRequested,
            CreatedAt = review.CreatedAt,
            Response = review.Response != null ? new ReviewResponseDto
            {
                Id = review.Response.Id,
                ReviewId = review.Response.ReviewId,
                SellerId = review.Response.SellerId,
                Content = review.Response.Content,
                SellerName = review.Response.SellerName,
                CreatedAt = review.Response.CreatedAt
            } : null
        };

        return Result<ReviewDto>.Success(dto);
    }
}

// ============================================================
// Admin query handlers
// ============================================================

/// <summary>
/// Handler para GetAdminReviewsQuery
/// </summary>
public class GetAdminReviewsQueryHandler : IRequestHandler<GetAdminReviewsQuery, Result<AdminReviewListDto>>
{
    private readonly IReviewRepository _reviewRepository;

    public GetAdminReviewsQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<Result<AdminReviewListDto>> Handle(GetAdminReviewsQuery request, CancellationToken cancellationToken)
    {
        var (reviews, totalCount) = await _reviewRepository.GetAdminReviewsAsync(
            request.Page,
            request.PageSize,
            request.Search,
            request.Status);

        var items = reviews.Select(r => MapToAdminDto(r));

        return Result<AdminReviewListDto>.Success(new AdminReviewListDto
        {
            Items = items,
            Total = totalCount
        });
    }

    internal static AdminReviewDto MapToAdminDto(ReviewService.Domain.Entities.Review r)
    {
        string status;
        if (r.IsApproved && !r.IsFlagged)
            status = "approved";
        else if (!r.IsApproved && r.ModeratedById == null)
            status = "pending";
        else
            status = "rejected";

        return new AdminReviewDto
        {
            Id = r.Id.ToString(),
            AuthorName = r.BuyerName,
            AuthorAvatar = r.BuyerPhotoUrl,
            TargetName = r.SellerId.ToString(),
            TargetType = "seller",
            Rating = r.Rating,
            Title = r.Title,
            Comment = r.Content,
            CreatedAt = r.CreatedAt.ToString("O"),
            Status = status,
            Reports = Array.Empty<string>()
        };
    }
}

/// <summary>
/// Handler para GetAdminReviewStatsQuery
/// </summary>
public class GetAdminReviewStatsQueryHandler : IRequestHandler<GetAdminReviewStatsQuery, Result<AdminReviewStatsDto>>
{
    private readonly IReviewRepository _reviewRepository;

    public GetAdminReviewStatsQueryHandler(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<Result<AdminReviewStatsDto>> Handle(GetAdminReviewStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = await _reviewRepository.GetAdminStatsAsync();

        return Result<AdminReviewStatsDto>.Success(new AdminReviewStatsDto
        {
            TotalReviews = stats.TotalReviews,
            PendingReviews = stats.PendingReviews,
            ApprovedReviews = stats.ApprovedReviews,
            AverageRating = stats.AverageRating,
            ReportedReviews = stats.FlaggedReviews
        });
    }
}