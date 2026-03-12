using MediaService.Domain.Entities;

namespace MediaService.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for media asset operations
/// </summary>
public interface IMediaRepository
{
    /// <summary>
    /// Gets a media asset by its unique identifier
    /// </summary>
    Task<MediaAsset?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all media assets for a specific owner
    /// </summary>
    Task<IEnumerable<MediaAsset>> GetByOwnerIdAsync(string ownerId, string? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets media assets by type
    /// </summary>
    Task<IEnumerable<MediaAsset>> GetByTypeAsync(Enums.MediaType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets media assets by status
    /// </summary>
    Task<IEnumerable<MediaAsset>> GetByStatusAsync(Enums.MediaStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unprocessed images for processing
    /// </summary>
    Task<IEnumerable<ImageMedia>> GetUnprocessedImagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unprocessed videos for processing
    /// </summary>
    Task<IEnumerable<VideoMedia>> GetUnprocessedVideosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated media assets with filtering and sorting
    /// </summary>
    Task<(IEnumerable<MediaAsset> items, int totalCount)> GetPaginatedAsync(
        string? ownerId = null,
        string? context = null,
        Enums.MediaType? type = null,
        Enums.MediaStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        bool sortDescending = true);

    /// <summary>
    /// Adds a new media asset
    /// </summary>
    Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing media asset
    /// </summary>
    Task UpdateAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a media asset
    /// </summary>
    Task DeleteAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple media assets
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<MediaAsset> mediaAssets, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a media asset exists with the given ID
    /// </summary>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of media assets
    /// </summary>
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about media assets
    /// </summary>
    Task<MediaStatistics> GetStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets media assets by status that were created before the cutoff date
    /// </summary>
    Task<IList<MediaAsset>> GetByStatusAndDateAsync(Enums.MediaStatus status, DateTime cutoffDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orphaned media assets not associated with any vehicle, older than the cutoff date
    /// </summary>
    Task<IList<MediaAsset>> GetOrphansOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all processed images with a CDN URL for health scanning.
    /// Returns batches for memory-efficient scanning.
    /// </summary>
    Task<IList<ImageMedia>> GetImagesForHealthScanAsync(int batchSize, int offset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of processed images that have a CDN URL.
    /// </summary>
    Task<int> GetProcessedImageCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of images currently marked as broken.
    /// </summary>
    Task<int> GetBrokenImageCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the top N dealers with the most broken images.
    /// Returns tuples of (DealerId, BrokenCount).
    /// </summary>
    Task<IList<(Guid DealerId, int BrokenCount)>> GetTopDealersWithBrokenImagesAsync(int topN, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-updates images as broken or healthy after a health scan batch.
    /// </summary>
    Task BulkUpdateImageHealthStatusAsync(IEnumerable<ImageMedia> images, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets per-owner (listing) broken image statistics for alerting.
    /// Groups processed images by OwnerId and counts total/broken per group.
    /// </summary>
    Task<IList<OwnerBrokenImageStats>> GetBrokenImageStatsByOwnerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets processed images that have fewer variants than expected.
    /// Returns batches of images with their current variant count for nightly validation.
    /// </summary>
    Task<IList<ImageVariantStatus>> GetImagesWithMissingVariantsAsync(
        string[] expectedVariantNames,
        int batchSize,
        int offset,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the top N listings (OwnerId) with the most broken images, sorted by broken count descending.
    /// Returns listing-level aggregated stats for the admin image health dashboard.
    /// </summary>
    Task<IList<ListingBrokenImageSummary>> GetTopListingsWithBrokenImagesAsync(
        int topN,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the aggregated image health summary for the admin dashboard.
    /// Single efficient query returning total images, broken count, last scan time, and storage bytes.
    /// </summary>
    Task<ImageHealthSummary> GetImageHealthSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the date and time of the last health scan across all images.
    /// </summary>
    Task<DateTime?> GetLastHealthScanTimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all storage keys (MediaAsset.StorageKey + MediaVariant.StorageKey) in batches.
    /// Used by the orphan cleanup job to compare against S3 objects.
    /// </summary>
    Task<IList<string>> GetAllStorageKeysAsync(int batchSize, int offset, CancellationToken cancellationToken = default);
}

/// <summary>
/// Per-owner (listing) broken image statistics for alerting.
/// </summary>
public class OwnerBrokenImageStats
{
    public string OwnerId { get; set; } = string.Empty;
    public Guid DealerId { get; set; }
    public int TotalImages { get; set; }
    public int BrokenCount { get; set; }
    public List<int> BrokenStatusCodes { get; set; } = new();
}

/// <summary>
/// Status of an image's variant generation for nightly validation.
/// </summary>
public class ImageVariantStatus
{
    public string MediaId { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public List<string> ExistingVariantNames { get; set; } = new();
    public List<string> MissingVariantNames { get; set; } = new();
}

/// <summary>
/// Listing-level broken image summary for admin dashboard top-20 table.
/// </summary>
public class ListingBrokenImageSummary
{
    public string OwnerId { get; set; } = string.Empty;
    public Guid DealerId { get; set; }
    public int TotalImages { get; set; }
    public int BrokenCount { get; set; }
    public double BrokenPercentage { get; set; }
    public DateTime? LastDetectedAt { get; set; }
}

/// <summary>
/// Aggregated image health summary for the admin dashboard header cards.
/// </summary>
public class ImageHealthSummary
{
    public int TotalActiveImages { get; set; }
    public int BrokenImages { get; set; }
    public int HealthyImages { get; set; }
    public double HealthPercentage { get; set; }
    public long TotalStorageBytes { get; set; }
    public DateTime? LastScanTime { get; set; }
    public int ImagesWithMissingVariants { get; set; }
}

/// <summary>
/// Statistics about media assets
/// </summary>
public class MediaStatistics
{
    public int TotalMedia { get; set; }
    public int Images { get; set; }
    public int Videos { get; set; }
    public int Documents { get; set; }
    public int Audio { get; set; }
    public int Processed { get; set; }
    public int Processing { get; set; }
    public int Failed { get; set; }
    public long TotalStorageBytes { get; set; }
    public DateTime? FirstMediaDate { get; set; }
    public DateTime? LastMediaDate { get; set; }
    public Dictionary<string, int> MediaByContext { get; set; } = new();
}