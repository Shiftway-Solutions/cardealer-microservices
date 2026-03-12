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