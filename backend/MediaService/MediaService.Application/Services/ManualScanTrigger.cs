namespace MediaService.Application.Services;

/// <summary>
/// Thread-safe static flag that allows the admin to trigger a manual health scan.
/// The ImageUrlHealthScanJob checks this flag every 30 seconds on its wait loop.
/// </summary>
public static class ManualScanTrigger
{
    private static int _scanRequested;

    /// <summary>
    /// Requests an immediate health scan. The job will pick this up within 30 seconds.
    /// </summary>
    public static void RequestScan() => Interlocked.Exchange(ref _scanRequested, 1);

    /// <summary>
    /// Atomically reads and clears the request flag. Returns true if a scan was requested.
    /// </summary>
    public static bool ConsumeRequest() => Interlocked.Exchange(ref _scanRequested, 0) == 1;
}
