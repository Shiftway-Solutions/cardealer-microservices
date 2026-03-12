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