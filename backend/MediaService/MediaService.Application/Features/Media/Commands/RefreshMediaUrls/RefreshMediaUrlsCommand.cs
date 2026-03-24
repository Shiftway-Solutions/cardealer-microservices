using MediaService.Shared;
using MediatR;

namespace MediaService.Application.Features.Media.Commands.RefreshMediaUrls;

/// <summary>
/// Admin command to bulk-refresh all stored CdnUrls for processed media assets.
/// Use this after IAM key rotation or when presigned URLs have expired.
/// </summary>
public record RefreshMediaUrlsCommand : IRequest<ApiResponse<RefreshMediaUrlsResponse>>
{
    /// <summary>
    /// Optional: limit refresh to a specific context (e.g., "vehicle-images").
    /// Null = refresh all contexts.
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Batch size for database updates. Default 100.
    /// </summary>
    public int BatchSize { get; init; } = 100;
}
