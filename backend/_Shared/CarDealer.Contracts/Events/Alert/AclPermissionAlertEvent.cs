using CarDealer.Contracts.Abstractions;

namespace CarDealer.Contracts.Events.Alert;

/// <summary>
/// Published when the weekly ACL verification job detects &gt;10 images
/// with incorrect permissions in a single day, indicating a possible
/// systemic issue in the image upload pipeline.
/// </summary>
public class AclPermissionAlertEvent : EventBase
{
    public override string EventType => "alert.media.acl_permission_violation";

    /// <summary>Total images scanned in this verification run</summary>
    public int TotalScanned { get; set; }

    /// <summary>Number of images found with incorrect (non-public-read) ACL</summary>
    public int IncorrectAclCount { get; set; }

    /// <summary>Number of images that were auto-corrected to public-read</summary>
    public int CorrectedCount { get; set; }

    /// <summary>Number of images that failed auto-correction</summary>
    public int FailedCorrectionCount { get; set; }

    /// <summary>Sample storage keys (first 20) of affected images</summary>
    public List<string> SampleAffectedKeys { get; set; } = new();

    /// <summary>Whether this is a systemic alert (>10 violations in a single day)</summary>
    public bool IsSystemicAlert { get; set; }

    /// <summary>Human-readable summary of the issue</summary>
    public string Summary { get; set; } = string.Empty;
}
