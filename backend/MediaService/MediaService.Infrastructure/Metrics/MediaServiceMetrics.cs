using System.Diagnostics.Metrics;

namespace MediaService.Infrastructure.Metrics;

public class MediaServiceMetrics
{
    private readonly Meter _meter;

    // Media Upload Metrics
    private readonly Counter<long> _mediaUploadsTotal;
    private readonly Counter<long> _mediaUploadsFailed;
    private readonly Histogram<double> _mediaUploadDuration;
    private readonly Histogram<double> _mediaFileSizeBytes;

    // Storage Metrics
    private readonly ObservableGauge<long> _totalStorageUsedBytes;
    private readonly Counter<long> _storageCleanupExecuted;

    // Image Processing Metrics
    private readonly Counter<long> _imageProcessingTotal;
    private readonly Counter<long> _thumbnailsGenerated;
    private readonly Histogram<double> _imageProcessingDuration;

    // Query Metrics
    private readonly Counter<long> _mediaQueriesTotal;
    private readonly Histogram<double> _mediaQueryDuration;

    public MediaServiceMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("MediaService");

        // Media Uploads
        _mediaUploadsTotal = _meter.CreateCounter<long>(
            "media_uploads_total",
            description: "Total number of media uploads");

        _mediaUploadsFailed = _meter.CreateCounter<long>(
            "media_uploads_failed_total",
            description: "Total number of failed media uploads");

        _mediaUploadDuration = _meter.CreateHistogram<double>(
            "media_upload_duration_seconds",
            unit: "s",
            description: "Duration of media upload operations");

        _mediaFileSizeBytes = _meter.CreateHistogram<double>(
            "media_file_size_bytes",
            unit: "bytes",
            description: "Distribution of uploaded file sizes");

        // Storage
        _totalStorageUsedBytes = _meter.CreateObservableGauge<long>(
            "media_storage_used_bytes",
            () => GetTotalStorageUsed(),
            unit: "bytes",
            description: "Total storage space used by media files");

        _storageCleanupExecuted = _meter.CreateCounter<long>(
            "media_storage_cleanup_total",
            description: "Number of storage cleanup operations executed");

        // Image Processing
        _imageProcessingTotal = _meter.CreateCounter<long>(
            "media_image_processing_total",
            description: "Total number of image processing operations");

        _thumbnailsGenerated = _meter.CreateCounter<long>(
            "media_thumbnails_generated_total",
            description: "Total number of thumbnails generated");

        _imageProcessingDuration = _meter.CreateHistogram<double>(
            "media_image_processing_duration_seconds",
            unit: "s",
            description: "Duration of image processing operations");

        // Queries
        _mediaQueriesTotal = _meter.CreateCounter<long>(
            "media_queries_total",
            description: "Total number of media queries executed");

        _mediaQueryDuration = _meter.CreateHistogram<double>(
            "media_query_duration_seconds",
            unit: "s",
            description: "Duration of media query operations");
    }

    // Media Upload Methods
    public void RecordMediaUpload(string mediaType, string fileExtension, long fileSizeBytes, double durationSeconds)
    {
        _mediaUploadsTotal.Add(1,
            new KeyValuePair<string, object?>("media_type", mediaType),
            new KeyValuePair<string, object?>("file_extension", fileExtension));

        _mediaUploadDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("media_type", mediaType));

        _mediaFileSizeBytes.Record(fileSizeBytes,
            new KeyValuePair<string, object?>("media_type", mediaType),
            new KeyValuePair<string, object?>("file_extension", fileExtension));
    }

    public void RecordMediaUploadFailed(string mediaType, string reason)
    {
        _mediaUploadsFailed.Add(1,
            new KeyValuePair<string, object?>("media_type", mediaType),
            new KeyValuePair<string, object?>("reason", reason));
    }

    // Storage Methods
    public void RecordStorageCleanup(int filesRemoved)
    {
        _storageCleanupExecuted.Add(1,
            new KeyValuePair<string, object?>("files_removed", filesRemoved));
    }

    private long GetTotalStorageUsed()
    {
        // Implementación placeholder - debe conectarse con servicio de almacenamiento real
        return 0;
    }

    // Image Processing Methods
    public void RecordImageProcessing(string operation, double durationSeconds)
    {
        _imageProcessingTotal.Add(1,
            new KeyValuePair<string, object?>("operation", operation));

        _imageProcessingDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("operation", operation));
    }

    public void RecordThumbnailGenerated(string size)
    {
        _thumbnailsGenerated.Add(1,
            new KeyValuePair<string, object?>("thumbnail_size", size));
    }

    // Query Methods
    public void RecordMediaQuery(string queryType, double durationSeconds)
    {
        _mediaQueriesTotal.Add(1,
            new KeyValuePair<string, object?>("query_type", queryType));

        _mediaQueryDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("query_type", queryType));
    }
}
