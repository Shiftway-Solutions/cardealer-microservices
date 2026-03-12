namespace AIProcessingService.Domain.Entities;

/// <summary>
/// Representa un trabajo de procesamiento de imagen con IA
/// </summary>
public class ImageProcessingJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }
    public Guid? UserId { get; set; }

    // Tipo de procesamiento
    public ProcessingType Type { get; set; }

    // URLs
    public string OriginalImageUrl { get; set; } = string.Empty;
    public string? ProcessedImageUrl { get; set; }
    public string? MaskUrl { get; set; }

    // Estado
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;

    // Opciones de procesamiento
    public ProcessingOptions Options { get; set; } = new();

    // Resultados
    public ProcessingResult? Result { get; set; }

    // Tiempos
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ProcessingTimeMs { get; set; }

    // Metadata
    public string? WorkerId { get; set; }
    public string? ModelVersion { get; set; }
}

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

public enum JobStatus
{
    Pending,
    Queued,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public class ProcessingOptions
{
    // Background replacement
    public string? BackgroundId { get; set; } = "white_studio";
    public string? CustomBackgroundUrl { get; set; }

    // License plate
    public bool MaskLicensePlate { get; set; } = true;
    public string? LicensePlateMaskType { get; set; } = "blur"; // blur, black, logo
    public string? LicensePlateLogoUrl { get; set; }

    // Shadow
    public bool GenerateShadow { get; set; } = true;
    public float ShadowOpacity { get; set; } = 0.4f;
    public int ShadowBlur { get; set; } = 50;

    // Color correction
    public bool AutoEnhance { get; set; } = true;
    public float Brightness { get; set; } = 1.0f;
    public float Contrast { get; set; } = 1.0f;
    public float Saturation { get; set; } = 1.0f;

    // Output
    public string OutputFormat { get; set; } = "jpg";
    public int OutputQuality { get; set; } = 90;
    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }
}

public class ProcessingResult
{
    // Segmentación
    public float SegmentationConfidence { get; set; }
    public BoundingBox? VehicleBoundingBox { get; set; }

    // Clasificación
    public string? ImageCategory { get; set; } // Exterior, Interior, Misc
    public float CategoryConfidence { get; set; }

    // Ángulo
    public string? DetectedAngle { get; set; } // front, rear, side, front-quarter, rear-quarter
    public float AngleConfidence { get; set; }

    // Placa
    public bool LicensePlateDetected { get; set; }
    public BoundingBox? LicensePlateBoundingBox { get; set; }

    // Calidad
    public int QualityScore { get; set; } // 0-100
    public List<string> QualityIssues { get; set; } = new();

    // Metadata de imagen
    public int OriginalWidth { get; set; }
    public int OriginalHeight { get; set; }
    public int ProcessedWidth { get; set; }
    public int ProcessedHeight { get; set; }
    public long FileSizeBytes { get; set; }
}

public class BoundingBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float Confidence { get; set; }
}
