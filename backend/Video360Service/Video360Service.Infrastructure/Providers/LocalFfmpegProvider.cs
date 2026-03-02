using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Video360Service.Domain.Enums;
using Video360Service.Domain.Interfaces;

namespace Video360Service.Infrastructure.Providers;

/// <summary>
/// Proveedor LOCAL - usa el binario ffmpeg instalado en el contenedor.
/// No requiere API key ni costo por video.
/// Este proveedor debe tener la máxima prioridad.
/// </summary>
public class LocalFfmpegProvider : IVideo360Provider
{
    private readonly ILogger<LocalFfmpegProvider> _logger;
    private readonly string _ffmpegPath;

    public Video360Provider ProviderType => Video360Provider.Local;
    public string ProviderName => "Local FFmpeg";
    public decimal CostPerVideoUsd => 0m;

    public LocalFfmpegProvider(ILogger<LocalFfmpegProvider> logger)
    {
        _logger = logger;
        // On Linux (Docker), ffmpeg is in PATH. On macOS dev, it may be in /opt/homebrew/bin or /usr/local/bin.
        _ffmpegPath = FindFfmpegPath();
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var available = File.Exists(_ffmpegPath) || IsInPath("ffmpeg");
        _logger.LogDebug("LocalFfmpegProvider available: {Available}, path: {Path}", available, _ffmpegPath);
        return Task.FromResult(available);
    }

    public async Task<Video360ExtractionResult> ExtractFramesAsync(
        byte[] videoBytes,
        Video360ExtractionOptions options,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var tempDir = Path.Combine(Path.GetTempPath(), $"v360_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);
            var inputPath = Path.Combine(tempDir, "input.mp4");
            var outputPattern = Path.Combine(tempDir, "frame_%02d.jpg");

            await File.WriteAllBytesAsync(inputPath, videoBytes, cancellationToken);

            // Get video duration first
            var duration = await GetVideoDurationAsync(inputPath, cancellationToken);
            var (width, height, fps) = await GetVideoInfoAsync(inputPath, cancellationToken);

            _logger.LogInformation("Video info: {Duration}s, {Width}x{Height}, {Fps}fps", duration, width, height, fps);

            // Calculate equidistant timestamps
            int frameCount = options.FrameCount > 0 ? options.FrameCount : 6;
            var timestamps = CalculateEquidistantTimestamps(duration, frameCount);

            // Extract frames using ffmpeg
            var extractSuccess = await ExtractFramesWithFfmpegAsync(
                inputPath, tempDir, timestamps, options, cancellationToken);

            if (!extractSuccess)
            {
                return new Video360ExtractionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "FFmpeg frame extraction failed",
                    ErrorCode = "FFMPEG_FAILED",
                    ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            // Read extracted frames
            var frames = new List<ExtractedFrameData>();
            for (int i = 0; i < frameCount; i++)
            {
                var framePath = Path.Combine(tempDir, $"frame_{i:D2}.jpg");

                if (!File.Exists(framePath))
                {
                    _logger.LogWarning("Frame file not found: {Path}", framePath);
                    continue;
                }

                var imageBytes = await File.ReadAllBytesAsync(framePath, cancellationToken);
                var angle = ExtractedFrameData.CalculateAngle(i, frameCount);

                frames.Add(new ExtractedFrameData
                {
                    Index = i,
                    AngleDegrees = angle,
                    TimestampSeconds = timestamps[i],
                    ImageBytes = imageBytes,
                    ContentType = "image/jpeg",
                    Width = options.Width ?? width,
                    Height = options.Height ?? height,
                    AngleLabel = Domain.Entities.ExtractedFrame.GetAngleLabelByIndex(i)
                });
            }

            if (frames.Count == 0)
            {
                return new Video360ExtractionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "No frames could be extracted from the video",
                    ErrorCode = "NO_FRAMES",
                    ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            return new Video360ExtractionResult
            {
                IsSuccess = true,
                Frames = frames,
                ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                CostUsd = 0m,
                VideoInfo = new Domain.Interfaces.VideoMetadata
                {
                    DurationSeconds = duration,
                    Width = width,
                    Height = height,
                    Fps = fps
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LocalFfmpegProvider extraction failed");
            return new Video360ExtractionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ErrorCode = "EXCEPTION",
                ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
        finally
        {
            // Cleanup temp directory
            try { Directory.Delete(tempDir, recursive: true); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to clean temp dir {Dir}", tempDir); }
        }
    }

    public async Task<Video360ExtractionResult> ExtractFramesFromUrlAsync(
        string videoUrl,
        Video360ExtractionOptions options,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var tempDir = Path.Combine(Path.GetTempPath(), $"v360_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);
            var inputPath = Path.Combine(tempDir, "input.mp4");

            // Download the video
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var videoBytes = await httpClient.GetByteArrayAsync(videoUrl, cancellationToken);
            await File.WriteAllBytesAsync(inputPath, videoBytes, cancellationToken);

            return await ExtractFramesAsync(videoBytes, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process video from URL {Url}", videoUrl);
            return new Video360ExtractionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ErrorCode = "URL_DOWNLOAD_FAILED",
                ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch { /* ignore */ }
        }
    }

    public Task<ProviderAccountInfo> GetAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ProviderAccountInfo
        {
            IsActive = true,
            CurrentPlan = "Local (Free)",
            RemainingCredits = int.MaxValue
        });
    }

    // ─── Private Helpers ────────────────────────────────────────────

    private async Task<bool> ExtractFramesWithFfmpegAsync(
        string inputPath,
        string outputDir,
        double[] timestamps,
        Video360ExtractionOptions options,
        CancellationToken cancellationToken)
    {
        // Build select filter: select frames at specific timestamps
        // select='eq(n\,0)+eq(n\,30)+...' approach is complex; better use -ss for each frame
        // For simplicity, extract ALL frames and pick equidistant ones, OR use multiple passes.
        // Most efficient: use the "select" filter with pts values.

        var frameCount = timestamps.Length;
        var scaleFilter = (options.Width.HasValue && options.Height.HasValue)
            ? $",scale={options.Width}:{options.Height}"
            : options.Width.HasValue
                ? $",scale={options.Width}:-2"
                : string.Empty;

        // Build select expression: eq(t,ts1)+eq(t,ts2)+... is not precise enough.
        // Better approach: extract using multiple -ss invocations (one frame per call)
        // Fast approach: single pass with select filter on timestamps

        // Use vsync=0 and frame index select to avoid duplicate frames
        var selectExpr = string.Join("+", timestamps.Select(ts => $"gte(t,{ts:F3})*lt(t,{ts + 0.05:F3})"));

        var args = $"-i \"{inputPath}\" -vf \"select='{selectExpr}'{scaleFilter}\" " +
                   $"-vsync 0 -q:v 2 -frames:v {frameCount} " +
                   $"\"{Path.Combine(outputDir, "frame_%02d.jpg")}\"";

        _logger.LogDebug("Running ffmpeg: {Args}", args);

        var success = await RunFfmpegAsync(args, cancellationToken);

        if (!success)
        {
            // Fallback: extract using individual -ss per frame (slower but more reliable)
            _logger.LogWarning("First ffmpeg approach failed, trying per-frame extraction");
            success = await ExtractFramesOneByOneAsync(inputPath, outputDir, timestamps, options, cancellationToken);
        }

        return success;
    }

    private async Task<bool> ExtractFramesOneByOneAsync(
        string inputPath,
        string outputDir,
        double[] timestamps,
        Video360ExtractionOptions options,
        CancellationToken cancellationToken)
    {
        var scaleFilter = options.Width.HasValue
            ? $"-vf \"scale={options.Width}:-2\""
            : string.Empty;

        for (int i = 0; i < timestamps.Length; i++)
        {
            var outputPath = Path.Combine(outputDir, $"frame_{i:D2}.jpg");
            var args = $"-ss {timestamps[i]:F3} -i \"{inputPath}\" " +
                       $"{scaleFilter} -frames:v 1 -q:v 2 \"{outputPath}\"";

            var success = await RunFfmpegAsync(args, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("Failed to extract frame {Index} at {Timestamp}s", i, timestamps[i]);
            }
        }

        // Check if at least some frames were extracted
        return Directory.GetFiles(outputDir, "frame_*.jpg").Length > 0;
    }

    private async Task<bool> RunFfmpegAsync(string args, CancellationToken cancellationToken)
    {
        try
        {
            var ffmpeg = GetFfmpegExecutable();
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("ffmpeg exited with code {Code}: {Stderr}", process.ExitCode, stderr[..Math.Min(500, stderr.Length)]);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run ffmpeg");
            return false;
        }
    }

    private async Task<double> GetVideoDurationAsync(string inputPath, CancellationToken cancellationToken)
    {
        try
        {
            var ffprobe = GetFfprobeExecutable();
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffprobe,
                    Arguments = $"-v quiet -print_format json -show_format \"{inputPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            using var doc = JsonDocument.Parse(output);
            if (doc.RootElement.TryGetProperty("format", out var format) &&
                format.TryGetProperty("duration", out var durationProp))
            {
                return double.Parse(durationProp.GetString() ?? "10");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get video duration, defaulting to 10s");
        }

        return 10.0; // default fallback
    }

    private async Task<(int width, int height, int fps)> GetVideoInfoAsync(string inputPath, CancellationToken cancellationToken)
    {
        try
        {
            var ffprobe = GetFfprobeExecutable();
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffprobe,
                    Arguments = $"-v quiet -print_format json -show_streams \"{inputPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            using var doc = JsonDocument.Parse(output);
            if (doc.RootElement.TryGetProperty("streams", out var streams))
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    if (stream.TryGetProperty("codec_type", out var codecType) &&
                        codecType.GetString() == "video")
                    {
                        var w = stream.TryGetProperty("width", out var wp) ? wp.GetInt32() : 1920;
                        var h = stream.TryGetProperty("height", out var hp) ? hp.GetInt32() : 1080;

                        // Parse fps from "r_frame_rate" like "30/1"
                        int fps = 30;
                        if (stream.TryGetProperty("r_frame_rate", out var fpsEl))
                        {
                            var fpsStr = fpsEl.GetString() ?? "30/1";
                            var parts = fpsStr.Split('/');
                            if (parts.Length == 2 && int.TryParse(parts[0], out var num) && int.TryParse(parts[1], out var den) && den > 0)
                                fps = num / den;
                        }

                        return (w, h, fps);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get video stream info");
        }

        return (1920, 1080, 30);
    }

    private static double[] CalculateEquidistantTimestamps(double duration, int frameCount)
    {
        // Skip the very beginning and end (first and last 5% of video)
        // to avoid black frames or transitions
        var startOffset = duration * 0.05;
        var endOffset = duration * 0.95;
        var effectiveDuration = endOffset - startOffset;
        var interval = effectiveDuration / (frameCount - 1);

        return Enumerable.Range(0, frameCount)
            .Select(i => Math.Round(startOffset + i * interval, 3))
            .ToArray();
    }

    private static string FindFfmpegPath()
    {
        // Check common paths
        string[] candidates = [
            "/usr/bin/ffmpeg",       // Debian/Ubuntu apt install
            "/usr/local/bin/ffmpeg", // Homebrew macOS
            "/opt/homebrew/bin/ffmpeg", // Homebrew Apple Silicon
            "ffmpeg"                 // PATH fallback
        ];

        foreach (var path in candidates)
        {
            if (path == "ffmpeg") return "ffmpeg"; // Let OS resolve
            if (File.Exists(path)) return path;
        }

        return "ffmpeg";
    }

    private string GetFfmpegExecutable() => _ffmpegPath;

    private static string GetFfprobeExecutable()
    {
        string[] candidates = [
            "/usr/bin/ffprobe",
            "/usr/local/bin/ffprobe",
            "/opt/homebrew/bin/ffprobe",
            "ffprobe"
        ];

        foreach (var path in candidates)
        {
            if (path == "ffprobe") return "ffprobe";
            if (File.Exists(path)) return path;
        }

        return "ffprobe";
    }

    private static bool IsInPath(string executable)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where" : "which",
                    Arguments = executable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
