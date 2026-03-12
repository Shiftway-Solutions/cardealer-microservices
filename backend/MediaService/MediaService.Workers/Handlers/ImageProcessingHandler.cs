using MediaService.Domain.Entities;
using MediaService.Domain.Interfaces.Repositories;
using MediaService.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace MediaService.Workers.Handlers;

/// <summary>
/// Handles asynchronous image processing after upload.
/// Generates exactly 3 WebP variants (thumbnail, medium, original) and stores them in S3.
/// Listens to RabbitMQ queue 'media.process' for ProcessMediaCommand messages.
/// </summary>
public class ImageProcessingHandler
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IImageProcessor _imageProcessor;
    private readonly IMediaStorageService _storageService;
    private readonly ILogger<ImageProcessingHandler> _logger;

    /// <summary>
    /// Exactly 3 required variants — all WebP with max file size enforcement.
    /// thumbnail: 300×200 px, max 30 KB
    /// medium:    800×600 px, max 150 KB
    /// original:  no resize (0×0), max 2 MB — format conversion to WebP only
    /// </summary>
    private static readonly ImageVariantDefinition[] DefaultVariants = new[]
    {
        new ImageVariantDefinition("thumbnail", 300, 200, 75, "image/webp", "Max", 30_720L),
        new ImageVariantDefinition("medium",    800, 600, 80, "image/webp", "Max", 153_600L),
        new ImageVariantDefinition("original",    0,   0, 85, "image/webp", "Max", 2_097_152L),
    };

    public ImageProcessingHandler(
        IMediaRepository mediaRepository,
        IImageProcessor imageProcessor,
        IMediaStorageService storageService,
        ILogger<ImageProcessingHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _imageProcessor = imageProcessor;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Process a single media asset: download original, generate variants, upload to S3, update DB.
    /// Called by the background worker when a message is received from RabbitMQ.
    /// </summary>
    public async Task HandleAsync(string mediaAssetId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting image processing for MediaAsset {MediaAssetId}", mediaAssetId);

        var mediaAsset = await _mediaRepository.GetByIdAsync(mediaAssetId, cancellationToken);
        if (mediaAsset == null)
        {
            _logger.LogWarning("MediaAsset {MediaAssetId} not found, skipping processing", mediaAssetId);
            return;
        }

        if (mediaAsset.Type != Domain.Enums.MediaType.Image)
        {
            _logger.LogInformation("MediaAsset {MediaAssetId} is not an image (Type={Type}), skipping", mediaAssetId, mediaAsset.Type);
            return;
        }

        if (mediaAsset.Status == Domain.Enums.MediaStatus.Processed)
        {
            _logger.LogInformation("MediaAsset {MediaAssetId} already processed, skipping", mediaAssetId);
            return;
        }

        mediaAsset.MarkAsProcessing();
        await _mediaRepository.UpdateAsync(mediaAsset, cancellationToken);

        try
        {
            var imageMedia = (ImageMedia)mediaAsset;
            var variantsGenerated = await GenerateVariantsAsync(imageMedia, cancellationToken);

            var cdnUrl = await _storageService.GetFileUrlAsync(mediaAsset.StorageKey);
            mediaAsset.MarkAsProcessed(cdnUrl);
            await _mediaRepository.UpdateAsync(mediaAsset, cancellationToken);

            _logger.LogInformation(
                "Successfully processed MediaAsset {MediaAssetId}: {VariantsGenerated} variants generated",
                mediaAssetId, variantsGenerated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process MediaAsset {MediaAssetId}", mediaAssetId);
            mediaAsset.MarkAsFailed($"Processing failed: {ex.Message}");
            await _mediaRepository.UpdateAsync(mediaAsset, cancellationToken);
            throw; // Re-throw for retry logic in the worker
        }
    }

    /// <summary>
    /// Generate all 3 WebP variant sizes for an image and upload them to S3.
    /// Uses CreateWebpVariantAsync with iterative quality reduction to respect max file size per variant.
    /// </summary>
    private async Task<int> GenerateVariantsAsync(ImageMedia imageMedia, CancellationToken cancellationToken)
    {
        using var originalStream = await _storageService.DownloadFileAsync(imageMedia.StorageKey);

        // Get original image info and update dimensions if needed
        var imageInfo = await _imageProcessor.GetImageInfoAsync(originalStream);
        if (imageMedia.Width <= 1 || imageMedia.Height <= 1)
        {
            imageMedia.SetDimensions(imageInfo.Width, imageInfo.Height);
        }

        var variantsGenerated = 0;
        var basePath = GetVariantBasePath(imageMedia.StorageKey);

        foreach (var variantDef in DefaultVariants)
        {
            try
            {
                // For sized variants (thumbnail, medium): skip if original is smaller than target
                if (variantDef.MaxWidth > 0 && variantDef.MaxHeight > 0
                    && variantDef.MaxWidth >= imageInfo.Width && variantDef.MaxHeight >= imageInfo.Height)
                {
                    _logger.LogDebug(
                        "Skipping variant {VariantName} for {MediaId} — original is smaller ({W}x{H})",
                        variantDef.Name, imageMedia.Id, imageInfo.Width, imageInfo.Height);
                    continue;
                }

                originalStream.Position = 0;

                using var variantStream = await _imageProcessor.CreateWebpVariantAsync(
                    originalStream,
                    variantDef.MaxWidth,
                    variantDef.MaxHeight,
                    variantDef.Quality,
                    variantDef.MaxFileSizeBytes,
                    variantDef.ResizeMode);

                var variantStorageKey = $"{basePath}/variants/{variantDef.Name}.webp";

                await _storageService.UploadFileAsync(variantStorageKey, variantStream, "image/webp");

                var variantUrl = await _storageService.GetFileUrlAsync(variantStorageKey);

                var mediaVariant = new MediaVariant(
                    imageMedia.Id,
                    variantDef.Name,
                    variantStorageKey,
                    variantDef.MaxWidth > 0 ? variantDef.MaxWidth : imageInfo.Width,
                    variantDef.MaxHeight > 0 ? variantDef.MaxHeight : imageInfo.Height,
                    variantStream.Length,
                    ".webp",
                    variantDef.Quality);

                mediaVariant.SetCdnUrl(variantUrl);
                imageMedia.AddVariant(mediaVariant);
                variantsGenerated++;

                _logger.LogDebug(
                    "Generated WebP variant {VariantName} ({Size} bytes, q{Quality}) for {MediaId}",
                    variantDef.Name, variantStream.Length, variantDef.Quality, imageMedia.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to create variant {VariantName} for MediaAsset {MediaId}",
                    variantDef.Name, imageMedia.Id);
                // Continue with other variants — don't fail the entire processing
            }
        }

        return variantsGenerated;
    }

    /// <summary>
    /// Extract the base path from a storage key for organizing variants.
    /// E.g., "vehicles/abc123/photo.jpg" → "vehicles/abc123/photo"
    /// </summary>
    private static string GetVariantBasePath(string storageKey)
    {
        var lastDot = storageKey.LastIndexOf('.');
        return lastDot > 0 ? storageKey[..lastDot] : storageKey;
    }

    /// <summary>
    /// Defines the configuration for a single image variant.
    /// MaxFileSizeBytes: 0 = no limit; >0 = quality will be reduced iteratively to fit.
    /// MaxWidth/MaxHeight: 0 = keep original dimensions (format conversion only).
    /// </summary>
    private record ImageVariantDefinition(
        string Name,
        int MaxWidth,
        int MaxHeight,
        int Quality,
        string ContentType,
        string ResizeMode,
        long MaxFileSizeBytes = 0);
}
