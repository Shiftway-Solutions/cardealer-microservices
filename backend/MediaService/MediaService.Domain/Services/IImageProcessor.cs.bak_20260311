namespace MediaService.Domain.Interfaces.Services;

/// <summary>
/// Interface for image processing operations
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Processes an image and creates variants
    /// </summary>
    Task<ImageProcessingResult> ProcessImageAsync(Stream imageStream, string originalFileName, IEnumerable<ImageVariantConfig> variants);

    /// <summary>
    /// Gets image information without processing
    /// </summary>
    Task<ImageInfo> GetImageInfoAsync(Stream imageStream);

    /// <summary>
    /// Validates if an image can be processed
    /// </summary>
    Task<bool> ValidateImageAsync(Stream imageStream, string contentType);

    /// <summary>
    /// Optimizes an image for web delivery
    /// </summary>
    Task<Stream> OptimizeImageAsync(Stream imageStream, string contentType, int quality);

    /// <summary>
    /// Creates a thumbnail from an image
    /// </summary>
    Task<Stream> CreateThumbnailAsync(Stream imageStream, int width, int height, string resizeMode = "Max");
}

/// <summary>
/// Image processing result
/// </summary>
public class ImageProcessingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ProcessedVariant> Variants { get; set; } = new();
    public ImageInfo OriginalImageInfo { get; set; } = new();
}

/// <summary>
/// Processed image variant
/// </summary>
public class ProcessedVariant
{
    public string Name { get; set; } = string.Empty;
    public Stream ImageStream { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string StorageKey { get; set; } = string.Empty;
}

/// <summary>
/// Image information
/// </summary>
public class ImageInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = string.Empty;
    public string ColorSpace { get; set; } = string.Empty;
    public bool HasAlpha { get; set; }
    public int Quality { get; set; }
    public long FileSize { get; set; }
}

/// <summary>
/// Image variant configuration
/// </summary>
public class ImageVariantConfig
{
    public string Name { get; set; } = string.Empty;
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    public int Quality { get; set; } = 85;
    public string Format { get; set; } = string.Empty;
    public bool MaintainAspectRatio { get; set; } = true;
    public string ResizeMode { get; set; } = "Max";
}