namespace MediaService.Domain.Entities;

/// <summary>
/// Represents an image media asset
/// </summary>
public class ImageMedia : MediaAsset
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string? HashSha256 { get; private set; }
    public bool IsPrimary { get; private set; }
    public string? AltText { get; private set; }

    /// <summary>
    /// Indicates whether the CDN/public URL for this image is broken (403, 404, 410, 500 or timeout).
    /// Set by the ImageUrlHealthScanJob background service.
    /// </summary>
    public bool BrokenImage { get; private set; }

    /// <summary>
    /// Timestamp of the last time BrokenImage was detected.
    /// Null if the image has never been detected as broken.
    /// </summary>
    public DateTime? BrokenImageDetectedAt { get; private set; }

    /// <summary>
    /// HTTP status code that caused the image to be flagged as broken.
    /// </summary>
    public int? BrokenImageHttpStatus { get; private set; }

    /// <summary>
    /// Timestamp of the last successful URL health check scan.
    /// </summary>
    public DateTime? LastHealthCheckAt { get; private set; }

    public ImageMedia(
        Guid dealerId,
        string ownerId,
        string? context,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        int width,
        int height)
        : base(dealerId, ownerId, context, Enums.MediaType.Image, originalFileName, contentType, sizeBytes, storageKey)
    {
        Width = width > 0 ? width : throw new ArgumentException("Width must be greater than 0", nameof(width));
        Height = height > 0 ? height : throw new ArgumentException("Height must be greater than 0", nameof(height));
    }

    public void SetDimensions(int width, int height)
    {
        if (width <= 0) throw new ArgumentException("Width must be greater than 0", nameof(width));
        if (height <= 0) throw new ArgumentException("Height must be greater than 0", nameof(height));

        Width = width;
        Height = height;
        MarkAsUpdated();
    }

    public void SetHash(string hash)
    {
        HashSha256 = hash ?? throw new ArgumentNullException(nameof(hash));
        MarkAsUpdated();
    }

    public void SetAsPrimary(bool isPrimary = true)
    {
        IsPrimary = isPrimary;
        MarkAsUpdated();
    }

    public void SetAltText(string altText)
    {
        AltText = altText;
        MarkAsUpdated();
    }

    /// <summary>
    /// Marks this image URL as broken with the HTTP status code that caused the failure.
    /// </summary>
    public void MarkAsBrokenImage(int httpStatusCode)
    {
        BrokenImage = true;
        BrokenImageDetectedAt = DateTime.UtcNow;
        BrokenImageHttpStatus = httpStatusCode;
        LastHealthCheckAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    /// <summary>
    /// Marks this image URL as healthy after a successful HEAD request.
    /// </summary>
    public void MarkAsHealthyImage()
    {
        BrokenImage = false;
        BrokenImageDetectedAt = null;
        BrokenImageHttpStatus = null;
        LastHealthCheckAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public double GetAspectRatio()
    {
        return Height > 0 ? (double)Width / Height : 0;
    }

    public string GetOrientation()
    {
        if (Width == Height) return "Square";
        return Width > Height ? "Landscape" : "Portrait";
    }

    public override bool IsValid()
    {
        return base.IsValid() &&
               Width > 0 &&
               Height > 0;
    }
}