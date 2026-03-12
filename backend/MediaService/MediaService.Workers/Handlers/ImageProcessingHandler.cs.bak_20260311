using MediaService.Domain.Entities;
using MediaService.Domain.Interfaces.Repositories;
using MediaService.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace MediaService.Workers.Handlers;

/// <summary>
/// Handles asynchronous image processing after upload.
/// Generates real variants (thumb, small, medium, large, webp) and stores them in S3.
/// Listens to RabbitMQ queue 'media.process' for ProcessMediaCommand messages.
/// </summary>
public class ImageProcessingHandler
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IImageProcessor _imageProcessor;
    private readonly IMediaStorageService _storageService;
    private readonly ILogger<ImageProcessingHandler> _logger;

    private static readonly ImageVariantDefinition[] DefaultVariants = new[]
    {
        new ImageVariantDefinition("thumb", 200, 200, 80, "image/jpeg", "Max"),
        new ImageVariantDefinition("small", 400, 400, 85, "image/jpeg", "Max"),
        new ImageVariantDefinition("medium", 800, 800, 85, "image/jpeg", "Max"),
        new ImageVariantDefinition("large", 1200, 1200, 90, "image/jpeg", "Max"),
        new ImageVariantDefinition("webp", 800, 800, 80, "image/webp", "Max"),
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
    /// Generate all variant sizes for an image and upload them to S3.
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
                // Skip generating variants larger than the original
                if (variantDef.MaxWidth >= imageInfo.Width && variantDef.MaxHeight >= imageInfo.Height
                    && variantDef.Name != "webp")
                {
                    _logger.LogDebug(
                        "Skipping variant {VariantName} for {MediaId} — original is smaller ({W}x{H})",
                        variantDef.Name, imageMedia.Id, imageInfo.Width, imageInfo.Height);
                    continue;
                }

                originalStream.Position = 0;

                using var variantStream = await _imageProcessor.CreateThumbnailAsync(
                    originalStream, variantDef.MaxWidth, variantDef.MaxHeight, variantDef.ResizeMode);

                var extension = variantDef.ContentType == "image/webp" ? ".webp" : ".jpg";
                var variantStorageKey = $"{basePath}/variants/{variantDef.Name}{extension}";

                await _storageService.UploadFileAsync(variantStorageKey, variantStream, variantDef.ContentType);

                var variantUrl = await _storageService.GetFileUrlAsync(variantStorageKey);

                var mediaVariant = new MediaVariant(
                    imageMedia.Id,
                    variantDef.Name,
                    variantStorageKey,
                    variantDef.MaxWidth,
                    variantDef.MaxHeight,
                    variantStream.Length,
                    extension,
                    variantDef.Quality);

                mediaVariant.SetCdnUrl(variantUrl);
                imageMedia.AddVariant(mediaVariant);
                variantsGenerated++;

                _logger.LogDebug(
                    "Generated variant {VariantName} ({Width}x{Height}, {Size} bytes) for {MediaId}",
                    variantDef.Name, variantDef.MaxWidth, variantDef.MaxHeight, variantStream.Length, imageMedia.Id);
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
    /// </summary>
    private record ImageVariantDefinition(
        string Name,
        int MaxWidth,
        int MaxHeight,
        int Quality,
        string ContentType,
        string ResizeMode);
}
