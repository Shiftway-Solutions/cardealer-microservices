using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AdminService.Application.Interfaces;

namespace AdminService.Infrastructure.External;

/// <summary>
/// Client for interacting with ReviewService admin endpoints
/// </summary>
public class ReviewServiceClient : IReviewServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ReviewServiceClient> _logger;
    private readonly string _baseUrl;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ReviewServiceClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ReviewServiceClient> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _baseUrl = configuration["ServiceUrls:ReviewService"]
            ?? Environment.GetEnvironmentVariable("REVIEWSERVICE_URL")
            ?? "http://reviewservice:8080";
    }

    /// <summary>
    /// Forwards the Authorization header from the incoming request
    /// </summary>
    private void ForwardAuthorizationHeader()
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                AuthenticationHeaderValue.Parse(authHeader);
        }
    }

    public async Task<AdminReviewListResult> GetAdminReviewsAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var qs = HttpUtility.ParseQueryString(string.Empty);
            qs["page"] = page.ToString();
            qs["pageSize"] = pageSize.ToString();
            if (!string.IsNullOrEmpty(search)) qs["search"] = search;
            if (!string.IsNullOrEmpty(status)) qs["status"] = status;

            var url = $"{_baseUrl}/api/reviews/admin?{qs}";
            _logger.LogInformation("Fetching admin reviews from ReviewService: {Url}", url);

            ForwardAuthorizationHeader();
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ReviewServiceListResponse>(_jsonOptions, cancellationToken);
                if (result != null)
                {
                    return new AdminReviewListResult
                    {
                        Items = result.Items?.Select(MapToDto).ToList() ?? new List<AdminReviewItemDto>(),
                        Total = result.Total
                    };
                }
            }
            else
            {
                _logger.LogWarning("ReviewService returned {StatusCode} for admin reviews list", response.StatusCode);
            }

            return new AdminReviewListResult { Items = new List<AdminReviewItemDto>(), Total = 0 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching admin reviews from ReviewService");
            return new AdminReviewListResult { Items = new List<AdminReviewItemDto>(), Total = 0 };
        }
    }

    public async Task<List<AdminReviewItemDto>> GetFlaggedReviewsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/reviews/admin/reported";
            _logger.LogInformation("Fetching reported reviews from ReviewService: {Url}", url);

            ForwardAuthorizationHeader();
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<ReviewServiceItemDto>>(_jsonOptions, cancellationToken);
                return result?.Select(MapToDto).ToList() ?? new List<AdminReviewItemDto>();
            }

            _logger.LogWarning("ReviewService returned {StatusCode} for reported reviews", response.StatusCode);
            return new List<AdminReviewItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reported reviews from ReviewService");
            return new List<AdminReviewItemDto>();
        }
    }

    public async Task<AdminReviewStatsResult> GetAdminReviewStatsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/api/reviews/admin/stats";
            _logger.LogInformation("Fetching review stats from ReviewService: {Url}", url);

            ForwardAuthorizationHeader();
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AdminReviewStatsResult>(_jsonOptions, cancellationToken);
                if (result != null)
                    return result;
            }

            _logger.LogWarning("ReviewService returned {StatusCode} for review stats", response.StatusCode);
            return new AdminReviewStatsResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching review stats from ReviewService");
            return new AdminReviewStatsResult();
        }
    }

    public async Task ApproveReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/reviews/{reviewId}/moderate?isApproved=true";
        _logger.LogInformation("Approving review {ReviewId} via ReviewService", reviewId);

        ForwardAuthorizationHeader();
        var response = await _httpClient.PostAsync(url, null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("ReviewService returned {StatusCode} when approving review {ReviewId}", response.StatusCode, reviewId);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task RejectReviewAsync(Guid reviewId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var qs = HttpUtility.ParseQueryString(string.Empty);
        qs["isApproved"] = "false";
        if (!string.IsNullOrEmpty(reason)) qs["rejectionReason"] = reason;

        var url = $"{_baseUrl}/api/reviews/{reviewId}/moderate?{qs}";
        _logger.LogInformation("Rejecting review {ReviewId} via ReviewService", reviewId);

        ForwardAuthorizationHeader();
        var response = await _httpClient.PostAsync(url, null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("ReviewService returned {StatusCode} when rejecting review {ReviewId}", response.StatusCode, reviewId);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task DeleteReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/reviews/admin/{reviewId}";
        _logger.LogInformation("Deleting review {ReviewId} via ReviewService (admin)", reviewId);

        ForwardAuthorizationHeader();
        var response = await _httpClient.DeleteAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("ReviewService returned {StatusCode} when deleting review {ReviewId}", response.StatusCode, reviewId);
            response.EnsureSuccessStatusCode();
        }
    }

    // =========================================================
    // Private mapping helpers
    // =========================================================

    private static AdminReviewItemDto MapToDto(ReviewServiceItemDto r) => new()
    {
        Id = r.Id ?? string.Empty,
        AuthorName = r.AuthorName ?? string.Empty,
        AuthorAvatar = r.AuthorAvatar,
        TargetName = r.TargetName ?? string.Empty,
        TargetType = r.TargetType ?? "seller",
        Rating = r.Rating,
        Title = r.Title ?? string.Empty,
        Comment = r.Comment ?? string.Empty,
        CreatedAt = r.CreatedAt ?? string.Empty,
        Status = r.Status ?? "pending",
        Reports = r.Reports ?? Array.Empty<string>()
    };
}

// =========================================================
// Internal response DTOs (matching ReviewService JSON output)
// =========================================================

internal class ReviewServiceListResponse
{
    public List<ReviewServiceItemDto>? Items { get; set; }
    public int Total { get; set; }
}

internal class ReviewServiceItemDto
{
    public string? Id { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorAvatar { get; set; }
    public string? TargetName { get; set; }
    public string? TargetType { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public string? CreatedAt { get; set; }
    public string? Status { get; set; }
    public string[]? Reports { get; set; }
}
