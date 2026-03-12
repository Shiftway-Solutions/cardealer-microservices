namespace AIProcessingService.Application.DTOs;

// ═══════════════════════════════════════════════════════════════════════════
// REQUEST DTOs
// ═══════════════════════════════════════════════════════════════════════════

public record ProcessImageRequest(
    Guid VehicleId,
    string ImageUrl,
    ProcessingType Type = ProcessingType.FullPipeline,
    ProcessingOptionsDto? Options = null
);

public record ProcessBatchRequest(
    Guid VehicleId,
    List<string> ImageUrls,
    ProcessingType Type = ProcessingType.FullPipeline,
    ProcessingOptionsDto? Options = null
);

public record Generate360Request(
    Guid VehicleId,
    Spin360SourceType SourceType,
    string? VideoUrl = null,
    List<string>? ImageUrls = null,
    int FrameCount = 36,
    Spin360OptionsDto? Options = null
);

public record AnalyzeImageRequest(
    string ImageUrl,
    Guid? VehicleId = null
);

/// <summary>
/// Callback request from Python workers (uses snake_case)
/// </summary>
public class ProcessingCallbackRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("job_id")]
    public string JobId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("success")]
    public bool Success { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("processed_url")]
    public string? ProcessedImageUrl { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("mask_url")]
    public string? MaskUrl { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("processing_time_ms")]
    public int? ProcessingTimeMs { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("error_message")]
    public string? Error { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

public record ProcessingOptionsDto(
    string? BackgroundId = "white_studio",
    string? CustomBackgroundUrl = null,
    bool MaskLicensePlate = true,
    string LicensePlateMaskType = "blur",
    bool GenerateShadow = true,
    bool AutoEnhance = true,
    string OutputFormat = "jpg",
    int OutputQuality = 90
);

public record Spin360OptionsDto(
    int TargetFrameCount = 36,
    string BackgroundId = "white_studio",
    bool ProcessFrames = true,
    bool MaskLicensePlate = true,
    bool GenerateShadows = true,
    int FrameWidth = 1920,
    int FrameHeight = 1080
);

public enum ProcessingType
{
    BackgroundRemoval = 0,      // Remove background (transparent PNG)
    VehicleSegmentation = 1,    // Just return mask
    ImageClassification = 2,
    AngleDetection = 3,
    LicensePlateMasking = 4,
    ColorCorrection = 5,
    QualityAnalysis = 6,
    BackgroundReplacement = 7,  // Replace background with preset
    FullPipeline = 8            // Full processing pipeline
}

public enum Spin360SourceType
{
    Video,
    Images
}

// ═══════════════════════════════════════════════════════════════════════════
// RESPONSE DTOs
// ═══════════════════════════════════════════════════════════════════════════

public record ProcessImageResponse(
    Guid JobId,
    string Status,
    string Message,
    string? StatusCheckUrl = null,
    int? EstimatedSeconds = null
);

public record ProcessBatchResponse(
    List<Guid> JobIds,
    int TotalImages,
    string Status,
    string Message,
    string? StatusCheckUrl = null,
    int? EstimatedSeconds = null
);

public record Generate360Response(
    Guid JobId,
    string Status,
    string Message,
    int TotalFrames,
    string? StatusCheckUrl = null,
    int? EstimatedMinutes = null
);

public record BatchProcessResponse(
    List<Guid> JobIds,
    int TotalImages,
    string Status,
    string Message,
    string? StatusCheckUrl = null,
    int? EstimatedSeconds = null
);

public record VehicleImagesResponse(
    Guid VehicleId,
    List<ProcessedImageDto> Images,
    int TotalImages
);

public record ProcessedImageDto(
    Guid JobId,
    string OriginalUrl,
    string? ProcessedUrl,
    string? ThumbnailUrl,
    string Status,
    string? ImageCategory,
    string? DetectedAngle,
    int QualityScore,
    DateTime ProcessedAt
);

public record QueueStatsResponse(
    int PendingJobs,
    int ProcessingJobs,
    int CompletedToday,
    int FailedToday,
    double AverageProcessingTimeMs,
    Dictionary<string, int> JobsByType
);

public record JobStatusResponse(
    Guid JobId,
    string Status,
    string? ErrorMessage = null,
    int ProgressPercent = 0,
    ProcessingResultDto? Result = null,
    DateTime CreatedAt = default,
    DateTime? CompletedAt = null,
    int ProcessingTimeMs = 0
);

public record Spin360StatusResponse(
    Guid JobId,
    string Status,
    string? ErrorMessage = null,
    int ProgressPercent = 0,
    int TotalFrames = 0,
    int ProcessedFrames = 0,
    Spin360ResultDto? Result = null,
    DateTime CreatedAt = default,
    DateTime? CompletedAt = null
);

public record ProcessingResultDto(
    string? ProcessedImageUrl,
    string? ImageCategory,
    float CategoryConfidence,
    string? DetectedAngle,
    float AngleConfidence,
    bool LicensePlateDetected,
    int QualityScore,
    List<string> QualityIssues
);

public record Spin360ResultDto(
    List<FrameDto> Frames,
    string? ViewerEmbedUrl,
    string? ThumbnailUrl,
    string? PreviewGifUrl,
    int TotalFrames,
    int DegreesPerFrame,
    float AverageQualityScore
);

public record FrameDto(
    int FrameNumber,
    int Degrees,
    string ProcessedUrl,
    string ThumbnailUrl,
    bool IsKeyFrame
);

// ═══════════════════════════════════════════════════════════════════════════
// BACKGROUND DTOs
// ═══════════════════════════════════════════════════════════════════════════

public record BackgroundDto(
    string Code,
    string Name,
    string Description,
    string ThumbnailUrl,
    string PreviewUrl,
    string Type,
    bool RequiresDealerMembership
);

public record AvailableBackgroundsResponse(
    List<BackgroundDto> Backgrounds,
    string DefaultBackgroundCode,
    bool HasPremiumAccess
);

// ═══════════════════════════════════════════════════════════════════════════
// FEATURES DTOs
// ═══════════════════════════════════════════════════════════════════════════

public record FeaturesResponse(
    string AccountType,
    bool HasActiveSubscription,
    FeaturesDto Features
);

public record FeaturesDto(
    FeatureAccessDto BackgroundReplacement,
    FeatureAccessDto Spin360,
    FeatureAccessDto VideoGeneration
);

public record FeatureAccessDto(
    bool Available,
    bool RequiresDealerMembership,
    string Description,
    List<string>? AvailableBackgrounds = null,
    string? DefaultBackground = null
);

// ═══════════════════════════════════════════════════════════════════════════
// QUEUE MESSAGE DTOs (para RabbitMQ)
// ═══════════════════════════════════════════════════════════════════════════

public record ImageProcessingMessage(
    Guid JobId,
    Guid VehicleId,
    string ImageUrl,
    ProcessingType Type,
    ProcessingOptionsDto Options,
    DateTime CreatedAt
);

// Message compatible with Python workers (snake_case properties)
public class PythonWorkerMessage
{
    public string job_id { get; set; } = "";
    public string vehicle_id { get; set; } = "";
    public string user_id { get; set; } = "";
    public string image_url { get; set; } = "";
    public string processing_type { get; set; } = "";
    public PythonWorkerOptions options { get; set; } = new();
}

public class PythonWorkerOptions
{
    public string background_id { get; set; } = "white_studio";
    public string? custom_background_url { get; set; }
    public bool mask_license_plate { get; set; } = true;
    public string license_plate_mask_type { get; set; } = "blur";
    public bool generate_shadow { get; set; } = true;
    public bool auto_enhance { get; set; } = true;
    public string output_format { get; set; } = "jpg";
    public int output_quality { get; set; } = 90;
}

public record Spin360ProcessingMessage(
    Guid JobId,
    Guid VehicleId,
    Spin360SourceType SourceType,
    string? VideoUrl,
    List<string>? ImageUrls,
    Spin360OptionsDto Options,
    DateTime CreatedAt
);

public record ProcessingCompletedMessage(
    Guid JobId,
    bool Success,
    string? ProcessedUrl,
    string? ErrorMessage,
    ProcessingResultDto? Result,
    int ProcessingTimeMs
);

public record Spin360CompletedMessage(
    Guid JobId,
    bool Success,
    string? ErrorMessage,
    Spin360ResultDto? Result,
    int ProcessingTimeMs
);
