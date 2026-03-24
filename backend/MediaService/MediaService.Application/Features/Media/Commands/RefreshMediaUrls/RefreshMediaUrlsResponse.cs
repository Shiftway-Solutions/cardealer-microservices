namespace MediaService.Application.Features.Media.Commands.RefreshMediaUrls;

public class RefreshMediaUrlsResponse
{
    /// <summary>Total media assets inspected.</summary>
    public int Inspected { get; set; }

    /// <summary>Assets whose CdnUrl was refreshed successfully.</summary>
    public int Refreshed { get; set; }

    /// <summary>Assets that failed to refresh (storage error).</summary>
    public int Failed { get; set; }

    /// <summary>Assets without a StorageKey (skipped).</summary>
    public int Skipped { get; set; }
}
