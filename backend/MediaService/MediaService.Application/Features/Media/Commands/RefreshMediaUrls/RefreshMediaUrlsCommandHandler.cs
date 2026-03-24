using MediaService.Domain.Interfaces.Repositories;
using MediaService.Domain.Interfaces.Services;
using MediaService.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MediaService.Application.Features.Media.Commands.RefreshMediaUrls;

/// <summary>
/// Bulk-refreshes CdnUrls for all processed media assets.
/// Solves the S3 presigned URL expiry problem: when UseAcl=false and
/// presigned URLs expire, this regenerates fresh URLs from the StorageKey.
/// </summary>
public class RefreshMediaUrlsCommandHandler
    : IRequestHandler<RefreshMediaUrlsCommand, ApiResponse<RefreshMediaUrlsResponse>>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IMediaStorageService _storageService;
    private readonly ILogger<RefreshMediaUrlsCommandHandler> _logger;

    public RefreshMediaUrlsCommandHandler(
        IMediaRepository mediaRepository,
        IMediaStorageService storageService,
        ILogger<RefreshMediaUrlsCommandHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<ApiResponse<RefreshMediaUrlsResponse>> Handle(
        RefreshMediaUrlsCommand request, CancellationToken cancellationToken)
    {
        var response = new RefreshMediaUrlsResponse();

        try
        {
            // Fetch all processed media assets
            var processedAssets = await _mediaRepository.GetByStatusAsync(
                MediaService.Domain.Enums.MediaStatus.Processed, cancellationToken);

            var assets = processedAssets
                .Where(a => request.Context == null ||
                            string.Equals(a.Context, request.Context, StringComparison.OrdinalIgnoreCase))
                .ToList();

            response.Inspected = assets.Count;
            _logger.LogInformation(
                "[RefreshUrls] Starting bulk URL refresh for {Count} processed media assets (context: {Context})",
                assets.Count, request.Context ?? "all");

            var batch = new List<Domain.Entities.MediaAsset>(request.BatchSize);

            foreach (var asset in assets)
            {
                if (string.IsNullOrEmpty(asset.StorageKey))
                {
                    response.Skipped++;
                    continue;
                }

                try
                {
                    var freshUrl = await _storageService.GetFileUrlAsync(asset.StorageKey);

                    // Only update if URL changed (avoids unnecessary DB writes)
                    if (freshUrl != asset.CdnUrl)
                    {
                        asset.MarkAsProcessed(freshUrl);
                        batch.Add(asset);
                        response.Refreshed++;
                    }
                    else
                    {
                        response.Skipped++;
                    }
                }
                catch (Exception ex)
                {
                    response.Failed++;
                    _logger.LogWarning(ex,
                        "[RefreshUrls] Failed to refresh URL for asset {Id} (key: {Key})",
                        asset.Id, asset.StorageKey);
                }

                // Flush batch to DB
                if (batch.Count >= request.BatchSize)
                {
                    await FlushBatchAsync(batch, cancellationToken);
                    batch.Clear();
                }
            }

            // Flush remaining
            if (batch.Count > 0)
                await FlushBatchAsync(batch, cancellationToken);

            _logger.LogInformation(
                "[RefreshUrls] Completed. Inspected={Inspected} Refreshed={Refreshed} Failed={Failed} Skipped={Skipped}",
                response.Inspected, response.Refreshed, response.Failed, response.Skipped);

            return ApiResponse<RefreshMediaUrlsResponse>.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RefreshUrls] Unhandled error during bulk URL refresh");
            return ApiResponse<RefreshMediaUrlsResponse>.Fail("Error durante refresh de URLs");
        }
    }

    private async Task FlushBatchAsync(
        List<Domain.Entities.MediaAsset> batch, CancellationToken cancellationToken)
    {
        foreach (var asset in batch)
        {
            try
            {
                await _mediaRepository.UpdateAsync(asset, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RefreshUrls] DB update failed for asset {Id}", asset.Id);
            }
        }
    }
}
