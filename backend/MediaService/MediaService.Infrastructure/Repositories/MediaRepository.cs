using MediaService.Domain.Entities;
using MediaService.Domain.Interfaces.Repositories;
using MediaService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Infrastructure.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly ApplicationDbContext _context;

    public MediaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MediaAsset?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .AsNoTracking()
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<MediaAsset>> GetByOwnerIdAsync(string ownerId, string? context = null, CancellationToken cancellationToken = default)
    {
        var query = _context.MediaAssets
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerId);

        if (!string.IsNullOrEmpty(context))
        {
            query = query.Where(x => x.Context == context);
        }

        return await query
            .Include(x => x.Variants)
            .OrderByDescending(x => x.CreatedAt)
            .Take(500) // Safety limit — use paginated endpoint for large datasets
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MediaAsset>> GetByTypeAsync(Domain.Enums.MediaType type, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .AsNoTracking()
            .Where(x => x.Type == type)
            .Include(x => x.Variants)
            .OrderByDescending(x => x.CreatedAt)
            .Take(500) // Safety limit
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MediaAsset>> GetByStatusAsync(Domain.Enums.MediaStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .AsNoTracking()
            .Where(x => x.Status == status)
            .Include(x => x.Variants)
            .OrderByDescending(x => x.CreatedAt)
            .Take(500) // Safety limit
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ImageMedia>> GetUnprocessedImagesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ImageMedia
            .AsNoTracking()
            .Where(x => x.Status == Domain.Enums.MediaStatus.Uploaded)
            .Include(x => x.Variants)
            .Take(100) // Process in batches
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<VideoMedia>> GetUnprocessedVideosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.VideoMedia
            .AsNoTracking()
            .Where(x => x.Status == Domain.Enums.MediaStatus.Uploaded)
            .Include(x => x.Variants)
            .Take(50) // Process in batches
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<MediaAsset> items, int totalCount)> GetPaginatedAsync(
        string? ownerId = null,
        string? context = null,
        Domain.Enums.MediaType? type = null,
        Domain.Enums.MediaStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        bool sortDescending = true)
    {
        var query = _context.MediaAssets.AsQueryable();

        if (!string.IsNullOrEmpty(ownerId))
        {
            query = query.Where(x => x.OwnerId == ownerId);
        }

        if (!string.IsNullOrEmpty(context))
        {
            query = query.Where(x => x.Context == context);
        }

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.AsNoTracking().CountAsync();

        // Sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            switch (sortBy.ToLower())
            {
                case "createdat":
                    query = sortDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt);
                    break;
                case "sizebytes":
                    query = sortDescending ? query.OrderByDescending(x => x.SizeBytes) : query.OrderBy(x => x.SizeBytes);
                    break;
                default:
                    query = sortDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt);
                    break;
            }
        }
        else
        {
            query = query.OrderByDescending(x => x.CreatedAt);
        }

        var items = await query
            .Include(x => x.Variants)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
    {
        _context.MediaAssets.Add(mediaAsset);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
    {
        _context.MediaAssets.Update(mediaAsset);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
    {
        _context.MediaAssets.Remove(mediaAsset);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<MediaAsset> mediaAssets, CancellationToken cancellationToken = default)
    {
        _context.MediaAssets.RemoveRange(mediaAssets);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .AsNoTracking()
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets.AsNoTracking().CountAsync(cancellationToken);
    }

    public async Task<MediaStatistics> GetStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.MediaAssets.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= toDate.Value);
        }

        // Performance: Consolidated single query instead of 11 separate round-trips
        var stats = await query
            .AsNoTracking()
            .GroupBy(x => 1) // Single group for aggregation
            .Select(g => new
            {
                TotalMedia = g.Count(),
                Images = g.Count(x => x.Type == Domain.Enums.MediaType.Image),
                Videos = g.Count(x => x.Type == Domain.Enums.MediaType.Video),
                Documents = g.Count(x => x.Type == Domain.Enums.MediaType.Document),
                Audio = g.Count(x => x.Type == Domain.Enums.MediaType.Audio),
                Processed = g.Count(x => x.Status == Domain.Enums.MediaStatus.Processed),
                Processing = g.Count(x => x.Status == Domain.Enums.MediaStatus.Processing),
                Failed = g.Count(x => x.Status == Domain.Enums.MediaStatus.Failed),
                TotalStorageBytes = g.Sum(x => x.SizeBytes),
                FirstMediaDate = g.Min(x => (DateTime?)x.CreatedAt),
                LastMediaDate = g.Max(x => (DateTime?)x.CreatedAt)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var mediaByContext = await query
            .AsNoTracking()
            .Where(x => x.Context != null)
            .GroupBy(x => x.Context!)
            .Select(g => new { Context = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Context, x => x.Count, cancellationToken);

        return new MediaStatistics
        {
            TotalMedia = stats?.TotalMedia ?? 0,
            Images = stats?.Images ?? 0,
            Videos = stats?.Videos ?? 0,
            Documents = stats?.Documents ?? 0,
            Audio = stats?.Audio ?? 0,
            Processed = stats?.Processed ?? 0,
            Processing = stats?.Processing ?? 0,
            Failed = stats?.Failed ?? 0,
            TotalStorageBytes = stats?.TotalStorageBytes ?? 0,
            FirstMediaDate = stats?.FirstMediaDate,
            LastMediaDate = stats?.LastMediaDate,
            MediaByContext = mediaByContext
        };
    }

    public async Task<IList<MediaAsset>> GetByStatusAndDateAsync(
        Domain.Enums.MediaStatus status, DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .Include(x => x.Variants)
            .Where(x => x.Status == status && x.CreatedAt < cutoffDate)
            .OrderBy(x => x.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public async Task<IList<MediaAsset>> GetOrphansOlderThanAsync(
        DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        return await _context.MediaAssets
            .Include(x => x.Variants)
            .Where(x => x.OwnerId == string.Empty && x.CreatedAt < cutoffDate)
            .OrderBy(x => x.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IList<ImageMedia>> GetImagesForHealthScanAsync(int batchSize, int offset, CancellationToken cancellationToken = default)
    {
        return await _context.ImageMedia
            .Where(x => x.Status == Domain.Enums.MediaStatus.Processed && x.CdnUrl != null)
            .OrderBy(x => x.CreatedAt)
            .Skip(offset)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetProcessedImageCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ImageMedia
            .AsNoTracking()
            .CountAsync(x => x.Status == Domain.Enums.MediaStatus.Processed && x.CdnUrl != null, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetBrokenImageCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ImageMedia
            .AsNoTracking()
            .CountAsync(x => x.BrokenImage, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IList<(Guid DealerId, int BrokenCount)>> GetTopDealersWithBrokenImagesAsync(int topN, CancellationToken cancellationToken = default)
    {
        var results = await _context.ImageMedia
            .AsNoTracking()
            .Where(x => x.BrokenImage)
            .GroupBy(x => x.DealerId)
            .Select(g => new { DealerId = g.Key, BrokenCount = g.Count() })
            .OrderByDescending(x => x.BrokenCount)
            .Take(topN)
            .ToListAsync(cancellationToken);

        return results.Select(r => (r.DealerId, r.BrokenCount)).ToList();
    }

    /// <inheritdoc/>
    public async Task BulkUpdateImageHealthStatusAsync(IEnumerable<ImageMedia> images, CancellationToken cancellationToken = default)
    {
        _context.ImageMedia.UpdateRange(images);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IList<OwnerBrokenImageStats>> GetBrokenImageStatsByOwnerAsync(CancellationToken cancellationToken = default)
    {
        var results = await _context.ImageMedia
            .AsNoTracking()
            .Where(x => x.Status == Domain.Enums.MediaStatus.Processed && x.CdnUrl != null)
            .GroupBy(x => new { x.OwnerId, x.DealerId })
            .Select(g => new
            {
                g.Key.OwnerId,
                g.Key.DealerId,
                TotalImages = g.Count(),
                BrokenCount = g.Count(x => x.BrokenImage),
                BrokenStatusCodes = g
                    .Where(x => x.BrokenImage && x.BrokenImageHttpStatus != null)
                    .Select(x => x.BrokenImageHttpStatus!.Value)
                    .Distinct()
                    .ToList()
            })
            .Where(x => x.BrokenCount > 0)
            .ToListAsync(cancellationToken);

        return results.Select(r => new OwnerBrokenImageStats
        {
            OwnerId = r.OwnerId,
            DealerId = r.DealerId,
            TotalImages = r.TotalImages,
            BrokenCount = r.BrokenCount,
            BrokenStatusCodes = r.BrokenStatusCodes
        }).ToList();
    }

    public async Task<IList<ImageVariantStatus>> GetImagesWithMissingVariantsAsync(
        string[] expectedVariantNames,
        int batchSize,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var images = await _context.Set<ImageMedia>()
            .AsNoTracking()
            .Include(x => x.Variants)
            .Where(x => x.Status == Domain.Enums.MediaStatus.Processed)
            .OrderBy(x => x.Id)
            .Skip(offset)
            .Take(batchSize)
            .Select(x => new
            {
                x.Id,
                x.StorageKey,
                x.OwnerId,
                ExistingVariantNames = x.Variants.Select(v => v.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        return images.Select(img =>
        {
            var missing = expectedVariantNames
                .Where(expected => !img.ExistingVariantNames.Contains(expected))
                .ToList();

            return new ImageVariantStatus
            {
                MediaId = img.Id,
                StorageKey = img.StorageKey,
                OwnerId = img.OwnerId,
                ExistingVariantNames = img.ExistingVariantNames,
                MissingVariantNames = missing
            };
        })
        .Where(x => x.MissingVariantNames.Count > 0)
        .ToList();
    }

    /// <inheritdoc/>
    public async Task<IList<ListingBrokenImageSummary>> GetTopListingsWithBrokenImagesAsync(
        int topN, CancellationToken cancellationToken = default)
    {
        return await _context.ImageMedia
            .AsNoTracking()
            .Where(x => x.Status == Domain.Enums.MediaStatus.Processed && x.CdnUrl != null)
            .GroupBy(x => new { x.OwnerId, x.DealerId })
            .Select(g => new ListingBrokenImageSummary
            {
                OwnerId = g.Key.OwnerId,
                DealerId = g.Key.DealerId,
                TotalImages = g.Count(),
                BrokenCount = g.Count(x => x.BrokenImage),
                BrokenPercentage = g.Count() > 0
                    ? Math.Round((double)g.Count(x => x.BrokenImage) / g.Count() * 100, 1)
                    : 0,
                LastDetectedAt = g.Max(x => x.BrokenImageDetectedAt)
            })
            .Where(x => x.BrokenCount > 0)
            .OrderByDescending(x => x.BrokenCount)
            .Take(topN)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ImageHealthSummary> GetImageHealthSummaryAsync(CancellationToken cancellationToken = default)
    {
        var stats = await _context.ImageMedia
            .AsNoTracking()
            .Where(x => x.Status == Domain.Enums.MediaStatus.Processed && x.CdnUrl != null)
            .GroupBy(x => 1)
            .Select(g => new
            {
                TotalActiveImages = g.Count(),
                BrokenImages = g.Count(x => x.BrokenImage),
                TotalStorageBytes = g.Sum(x => x.SizeBytes),
                LastScanTime = g.Max(x => x.LastHealthCheckAt)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalActive = stats?.TotalActiveImages ?? 0;
        var broken = stats?.BrokenImages ?? 0;

        return new ImageHealthSummary
        {
            TotalActiveImages = totalActive,
            BrokenImages = broken,
            HealthyImages = totalActive - broken,
            HealthPercentage = totalActive > 0
                ? Math.Round((double)(totalActive - broken) / totalActive * 100, 2)
                : 100,
            TotalStorageBytes = stats?.TotalStorageBytes ?? 0,
            LastScanTime = stats?.LastScanTime
        };
    }

    /// <inheritdoc/>
    public async Task<DateTime?> GetLastHealthScanTimeAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ImageMedia
            .AsNoTracking()
            .Where(x => x.LastHealthCheckAt != null)
            .MaxAsync(x => x.LastHealthCheckAt, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IList<string>> GetAllStorageKeysAsync(int batchSize, int offset, CancellationToken cancellationToken = default)
    {
        // Get MediaAsset storage keys
        var assetKeys = await _context.MediaAssets
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip(offset)
            .Take(batchSize)
            .Select(x => x.StorageKey)
            .ToListAsync(cancellationToken);

        if (assetKeys.Count == 0)
            return assetKeys;

        // Also get variant storage keys for the same batch range
        var variantKeys = await _context.Set<MediaService.Domain.Entities.MediaVariant>()
            .AsNoTracking()
            .Where(v => v.StorageKey != null && v.StorageKey != string.Empty)
            .OrderBy(v => v.Id)
            .Skip(offset)
            .Take(batchSize)
            .Select(v => v.StorageKey)
            .ToListAsync(cancellationToken);

        var allKeys = new List<string>(assetKeys.Count + variantKeys.Count);
        allKeys.AddRange(assetKeys);
        allKeys.AddRange(variantKeys);
        return allKeys;
    }
}