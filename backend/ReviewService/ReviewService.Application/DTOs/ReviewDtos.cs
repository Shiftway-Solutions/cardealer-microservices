namespace ReviewService.Application.DTOs;

/// <summary>
/// DTO para mostrar una review en el frontend
/// </summary>
public record ReviewDto
{
    public Guid Id { get; init; }
    public Guid BuyerId { get; init; }
    public Guid SellerId { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? OrderId { get; init; }
    public int Rating { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsVerifiedPurchase { get; init; }
    public string BuyerName { get; init; } = string.Empty;
    public string? BuyerPhotoUrl { get; init; }
    public DateTime CreatedAt { get; init; }

    // Respuesta del vendedor (Sprint 15 - Nueva funcionalidad)
    public string? SellerResponse { get; init; }
    public DateTime? SellerRespondedAt { get; init; }

    // Votos útiles (Sprint 15 - Nueva funcionalidad)
    public ReviewVoteStatsDto? VoteStats { get; init; }
    public bool? UserVotedHelpful { get; init; }  // Para mostrar si el usuario actual votó

    // Anti-fraude (Sprint 15 - Nueva funcionalidad)
    public decimal TrustScore { get; init; }
    public bool WasAutoRequested { get; init; }

    // Respuesta del vendedor (si existe) - DEPRECATED, use SellerResponse
    public ReviewResponseDto? Response { get; init; }
}

/// <summary>
/// DTO para crear una nueva review
/// </summary>
public record CreateReviewDto
{
    public Guid SellerId { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? OrderId { get; init; }
    public int Rating { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// DTO para actualizar una review
/// </summary>
public record UpdateReviewDto
{
    public int Rating { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// DTO para respuesta de vendedor a review
/// </summary>
public record ReviewResponseDto
{
    public Guid Id { get; init; }
    public Guid ReviewId { get; init; }
    public Guid SellerId { get; init; }
    public string Content { get; init; } = string.Empty;
    public string SellerName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO para crear respuesta de vendedor
/// </summary>
public record CreateReviewResponseDto
{
    public Guid ReviewId { get; init; }
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// DTO para estadísticas de reviews de un vendedor
/// </summary>
public record ReviewSummaryDto
{
    public Guid SellerId { get; init; }
    public int TotalReviews { get; init; }
    public decimal AverageRating { get; init; }
    public Dictionary<int, int> RatingDistribution { get; init; } = new();
    public decimal PositivePercentage { get; init; }
    public int VerifiedPurchaseReviews { get; init; }
    public DateTime? LastReviewDate { get; init; }
}

/// <summary>
/// DTO para resultados paginados de reviews
/// </summary>
public record PagedReviewsDto
{
    public IEnumerable<ReviewDto> Reviews { get; init; } = new List<ReviewDto>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

/// <summary>
/// DTO para filtros de reviews
/// </summary>
public record ReviewFiltersDto
{
    public int? Rating { get; init; }
    public bool? OnlyVerified { get; init; }
    public string? SearchTerm { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

/// <summary>
/// Sprint 15 - DTO para estadísticas de votos de utilidad
/// </summary>
public record ReviewVoteStatsDto
{
    public Guid ReviewId { get; init; }
    public int HelpfulVotes { get; init; }
    public int TotalVotes { get; init; }
    public decimal HelpfulPercentage { get; init; }
    public bool? CurrentUserVotedHelpful { get; init; }
}

/// <summary>
/// Sprint 15 - DTO para crear solicitud de review
/// </summary>
public record CreateReviewRequestDto
{
    public Guid BuyerId { get; init; }
    public Guid SellerId { get; init; }
    public Guid VehicleId { get; init; }
    public Guid OrderId { get; init; }
    public string BuyerEmail { get; init; } = string.Empty;
    public string BuyerName { get; init; } = string.Empty;
    public string VehicleTitle { get; init; } = string.Empty;
    public string SellerName { get; init; } = string.Empty;
    public DateTime PurchaseDate { get; init; }
}

/// <summary>
/// Sprint 15 - DTO para votar utilidad
/// </summary>
public record VoteHelpfulDto
{
    public bool IsHelpful { get; init; }
}

/// <summary>
/// Sprint 15 - DTO para badge de vendedor
/// </summary>
public record SellerBadgeDto
{
    public Guid Id { get; init; }
    public Guid SellerId { get; init; }
    public string BadgeType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public DateTime EarnedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Sprint 15 - DTO para resultado de cálculo de badges
/// </summary>
public record BadgeCalculationResultDto
{
    public Guid? SellerId { get; init; }
    public int ProcessedSellers { get; init; }
    public int TotalBadgesGranted { get; init; }
    public int TotalBadgesRevoked { get; init; }
    public List<SellerBadgeDto> NewBadges { get; init; } = new();
    public List<string> RevokedBadges { get; init; } = new();
    public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Sprint 15 - DTO para resultado de actualización de badges
/// </summary>
public record BadgeUpdateResultDto
{
    public Guid SellerId { get; init; }
    public List<SellerBadgeDto> CurrentBadges { get; init; } = new();
    public List<string> NewBadgesEarned { get; init; } = new();
    public List<string> BadgesRevoked { get; init; } = new();
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Sprint 15 - DTO para voto de utilidad de review
/// </summary>
public record ReviewHelpfulVoteDto
{
    public Guid Id { get; init; }
    public Guid ReviewId { get; init; }
    public Guid UserId { get; init; }
    public bool IsHelpful { get; init; }
    public DateTime VotedAt { get; init; }
}

/// <summary>
/// Sprint 15 - DTO para solicitud de review pendiente
/// </summary>
public record PendingReviewRequestDto
{
    public Guid Id { get; init; }
    public Guid BuyerId { get; init; }
    public Guid SellerId { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? OrderId { get; init; }
    public string BuyerEmail { get; init; } = string.Empty;
    public string BuyerName { get; init; } = string.Empty;
    public string VehicleTitle { get; init; } = string.Empty;
    public string SellerName { get; init; } = string.Empty;
    public DateTime PurchaseDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public int RemindersSent { get; init; }
    public DateTime? LastReminderSentAt { get; init; }
}

// ============================================================
// Admin DTOs
// ============================================================

/// <summary>
/// DTO de review para el panel de administración
/// </summary>
public record AdminReviewDto
{
    public string Id { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string? AuthorAvatar { get; init; }
    public string TargetName { get; init; } = string.Empty;
    public string TargetType { get; init; } = "seller";
    public int Rating { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string Status { get; init; } = "pending";
    public string[] Reports { get; init; } = Array.Empty<string>();
}

/// <summary>
/// DTO de review reportada para el panel de administración
/// </summary>
public record ReportedAdminReviewDto : AdminReviewDto
{
    public int ReportCount { get; init; }
    public string[] ReportReasons { get; init; } = Array.Empty<string>();
    public string LastReportedAt { get; init; } = string.Empty;
}

/// <summary>
/// Lista paginada de reviews para el panel de administración
/// </summary>
public record AdminReviewListDto
{
    public IEnumerable<AdminReviewDto> Items { get; init; } = Enumerable.Empty<AdminReviewDto>();
    public int Total { get; init; }
}

/// <summary>
/// Estadísticas globales de reviews para el panel de administración
/// </summary>
public record AdminReviewStatsDto
{
    public int TotalReviews { get; init; }
    public int PendingReviews { get; init; }
    public int ApprovedReviews { get; init; }
    public decimal AverageRating { get; init; }
    public int ReportedReviews { get; init; }
}