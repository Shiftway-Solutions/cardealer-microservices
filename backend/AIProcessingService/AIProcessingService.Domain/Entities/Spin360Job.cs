namespace AIProcessingService.Domain.Entities;

/// <summary>
/// Trabajo de generación de 360° Spin
/// </summary>
public class Spin360Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }
    public Guid? UserId { get; set; }

    // Tipo de fuente
    public Spin360SourceType SourceType { get; set; }

    // Si es desde video
    public string? SourceVideoUrl { get; set; }
    public int FrameCount { get; set; } = 36;

    // Si es desde imágenes
    public List<string> SourceImageUrls { get; set; } = new();

    // Estado
    public Spin360Status Status { get; set; } = Spin360Status.Pending;
    public string? ErrorMessage { get; set; }

    // Opciones
    public Spin360Options Options { get; set; } = new();

    // Resultado
    public Spin360Result? Result { get; set; }

    // Progreso
    public int TotalFrames { get; set; }
    public int ProcessedFrames { get; set; }
    public int ProgressPercent => TotalFrames > 0 ? (ProcessedFrames * 100) / TotalFrames : 0;

    // Tiempos
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ProcessingTimeMs { get; set; }

    // Metadata
    public string? WorkerId { get; set; }
}

public enum Spin360SourceType
{
    Video,
    Images
}

public enum Spin360Status
{
    Pending,
    ExtractingFrames,
    ProcessingFrames,
    GeneratingViewer,
    Completed,
    Failed
}

public class Spin360Options
{
    // Frame extraction
    public int TargetFrameCount { get; set; } = 36;

    // Background
    public string BackgroundId { get; set; } = "white_studio";
    public string? CustomBackgroundUrl { get; set; }

    // Processing
    public bool ProcessFrames { get; set; } = true;
    public bool MaskLicensePlate { get; set; } = true;
    public bool GenerateShadows { get; set; } = true;
    public bool AutoEnhance { get; set; } = true;

    // Output
    public int FrameWidth { get; set; } = 1920;
    public int FrameHeight { get; set; } = 1080;
    public int FrameQuality { get; set; } = 85;

    // Viewer
    public bool GenerateViewerEmbed { get; set; } = true;
    public bool EnableHotspots { get; set; } = true;
}

public class Spin360Result
{
    // Frames procesados
    public List<ProcessedFrame> Frames { get; set; } = new();

    // URLs de viewer
    public string? ViewerEmbedUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? PreviewGifUrl { get; set; }

    // Metadata
    public int TotalFrames { get; set; }
    public int DegreesPerFrame { get; set; }
    public string ViewerType { get; set; } = "custom"; // custom, embed

    // Calidad promedio
    public float AverageQualityScore { get; set; }
}

public class ProcessedFrame
{
    public int FrameNumber { get; set; }
    public int Degrees { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ProcessedUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public float QualityScore { get; set; }
    public bool IsKeyFrame { get; set; }
}
