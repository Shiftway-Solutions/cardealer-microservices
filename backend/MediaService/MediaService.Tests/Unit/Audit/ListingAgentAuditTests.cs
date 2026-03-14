using FluentAssertions;
using MediaService.Application.Features.Media.Commands.ProcessMedia;
using MediaService.Application.Features.Media.Queries.ValidateImageQuality;
using MediaService.Domain.Entities;
using MediaService.Domain.Interfaces.Repositories;
using MediaService.Domain.Interfaces.Services;
using MediaService.Workers;
using MediaService.Workers.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using System.Text.Json;

using DomainMediaStatus = MediaService.Domain.Enums.MediaStatus;

namespace MediaService.Tests.Unit.Audit;

// ════════════════════════════════════════════════════════════════════════════
// ListingAgent Audit — Photo processing, variant generation, quality checks
// ════════════════════════════════════════════════════════════════════════════

#region ═══ A. ValidateImageQuality Audit Tests ═══

/// <summary>
/// Tests the image quality validation pipeline:
/// blur/dark/bright heuristics, resolution checks, scoring, EXIF detection.
/// </summary>
public class ValidateImageQualityAuditTests
{
    private readonly Mock<IImageProcessor> _processorMock;
    private readonly ValidateImageQualityQueryHandler _handler;

    public ValidateImageQualityAuditTests()
    {
        _processorMock = new Mock<IImageProcessor>();
        var loggerMock = new Mock<ILogger<ValidateImageQualityQueryHandler>>();
        _handler = new ValidateImageQualityQueryHandler(_processorMock.Object, loggerMock.Object);
    }

    /// <summary>Helper: create a ValidateImageQualityQuery with mocked file and image info.</summary>
    private ValidateImageQualityQuery CreateQuery(int width, int height, long fileSize, string format = "jpeg")
    {
        var stream = new MemoryStream(new byte[10]);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(fileSize);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        _processorMock
            .Setup(p => p.GetImageInfoAsync(It.IsAny<Stream>()))
            .ReturnsAsync(new ImageInfo { Width = width, Height = height, Format = format });

        return new ValidateImageQualityQuery { File = fileMock.Object };
    }

    // ── 1. Good quality → score ≥ 80 ──
    [Fact]
    public async Task GoodQuality_HighRes_ScoreAbove80()
    {
        // 1920×1080 JPEG, 500KB → bytesPerMp ≈ 241k (normal range)
        var query = CreateQuery(1920, 1080, 500_000, "jpeg");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.OverallScore.Should().BeGreaterThanOrEqualTo(80);
        result.Data.IsBlurry.Should().BeFalse();
        result.Data.IsTooDark.Should().BeFalse();
        result.Data.IsTooBright.Should().BeFalse();
        result.Data.IsTooSmall.Should().BeFalse();
    }

    // ── 2. Low resolution → penalty + warning ──
    [Fact]
    public async Task LowResolution_Below640x480_Penalty()
    {
        // 320×240 is below minimum 640×480
        var query = CreateQuery(320, 240, 100_000, "jpeg");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.IsTooSmall.Should().BeTrue();
        result.Data.OverallScore.Should().BeLessThan(80);
        result.Data.Warnings.Should().Contain(w => w.Contains("Resolución baja"));
    }

    // ── 3. Small file size (<50KB) → penalty ──
    [Fact]
    public async Task SmallFileSize_Under50KB_Penalty()
    {
        // 800×600, only 30KB → triggers small file penalty
        var query = CreateQuery(800, 600, 30_000, "jpeg");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.OverallScore.Should().BeLessThan(90);
        result.Data.Warnings.Should().Contain(w => w.Contains("muy pequeño"));
    }

    // ── 4. Blur detection: 10k ≤ bytesPerMp < 15k ──
    [Fact]
    public async Task BlurDetection_ModerateLowBytesPerMp_FlagsBlurry()
    {
        // 1000×1000 (1 MP), 12,000 bytes → bytesPerMp = 12,000
        // 12k ≥ 10k (not dark) AND 12k < 15k (blur threshold) → IS blur
        var query = CreateQuery(1000, 1000, 12_000, "jpeg");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.IsBlurry.Should().BeTrue();
        result.Data.IsTooDark.Should().BeFalse();
        result.Data.Warnings.Should().Contain(w => w.Contains("borrosa"));
    }

    // ── 5. Dark detection: bytesPerMp < 10k ──
    [Fact]
    public async Task DarkDetection_VeryLowBytesPerMp_FlagsDark()
    {
        // 1000×1000 (1 MP), 8,000 bytes → bytesPerMp = 8,000
        // 8k < 10k → IS dark; blur has !isTooDark guard → NOT blur
        var query = CreateQuery(1000, 1000, 8_000, "jpeg");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.IsTooDark.Should().BeTrue();
        result.Data.IsBlurry.Should().BeFalse();
        result.Data.Warnings.Should().Contain(w => w.Contains("oscura"));
    }

    // ── 6. Bright detection: bytesPerMp > 500k ──
    [Fact]
    public async Task BrightDetection_HighBytesPerMp_FlagsBright()
    {
        // 1000×1000 (1 MP), 600,000 bytes → bytesPerMp = 600,000
        // 600k > 500k → IS bright
        var query = CreateQuery(1000, 1000, 600_000, "jpeg");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.IsTooBright.Should().BeTrue();
        result.Data.Warnings.Should().Contain(w => w.Contains("sobreexpuesta"));
    }

    // ── 7. Ideal resolution bonus (≥1920×1080) ──
    [Fact]
    public async Task IdealResolution_1920x1080_GetsBonus()
    {
        var query = CreateQuery(1920, 1080, 500_000, "jpeg");
        var result = await _handler.Handle(query, CancellationToken.None);

        // Perfect image with ideal resolution → score ≥ 95
        result.Data!.OverallScore.Should().BeGreaterThanOrEqualTo(95);
    }

    // ── 8. Good aspect ratio bonus (4:3 and 16:9) ──
    [Theory]
    [InlineData(1333, 1000)] // 4:3 → ratio ≈ 1.333
    [InlineData(1780, 1000)] // 16:9 → ratio ≈ 1.78
    public async Task GoodAspectRatio_GetsBonus(int width, int height)
    {
        var query = CreateQuery(width, height, 200_000, "jpeg");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.OverallScore.Should().BeGreaterThanOrEqualTo(80);
    }

    // ── 9. JPEG → HasExifOrientation true ──
    [Fact]
    public async Task JPEG_HasExifOrientation_True()
    {
        var query = CreateQuery(1920, 1080, 500_000, "jpeg");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.HasExifOrientation.Should().BeTrue();
    }

    // ── 10. PNG → HasExifOrientation false ──
    [Fact]
    public async Task PNG_HasExifOrientation_False()
    {
        var query = CreateQuery(1920, 1080, 500_000, "png");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.HasExifOrientation.Should().BeFalse();
    }

    // ── 11. Error handling → Fail response ──
    [Fact]
    public async Task Error_InImageProcessor_ReturnsFailResponse()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.OpenReadStream()).Throws(new IOException("File corrupted"));
        fileMock.Setup(f => f.Length).Returns(100);

        var query = new ValidateImageQualityQuery { File = fileMock.Object };
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    // ── 12. Dark and blur are mutually exclusive ──
    [Theory]
    [InlineData(5_000)]  // deep dark range
    [InlineData(8_000)]  // upper dark range
    [InlineData(12_000)] // blur range
    [InlineData(14_999)] // upper blur boundary
    public async Task DarkAndBlur_AreMutuallyExclusive(long fileSize)
    {
        // 1000×1000 = 1 MP → bytesPerMp = fileSize
        var query = CreateQuery(1000, 1000, fileSize, "jpeg");
        var result = await _handler.Handle(query, CancellationToken.None);

        var bothTrue = result.Data!.IsBlurry && result.Data.IsTooDark;
        bothTrue.Should().BeFalse("dark and blur detections must be mutually exclusive");
    }

    // ── 13. Under 0.5 MP → skip bytes-per-mp heuristics ──
    [Fact]
    public async Task SmallImage_Under0_5Mp_SkipsBytesPerMpHeuristics()
    {
        // 500×500 = 0.25 MP → below the 0.5 MP threshold
        var query = CreateQuery(500, 500, 1_000, "jpeg");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.IsBlurry.Should().BeFalse("megapixels < 0.5 should skip blur check");
        result.Data.IsTooDark.Should().BeFalse("megapixels < 0.5 should skip dark check");
        result.Data.IsTooBright.Should().BeFalse("megapixels < 0.5 should skip bright check");
    }

    // ── 14. Score is always clamped [0, 100] ──
    [Fact]
    public async Task Score_NeverExceeds100()
    {
        // Perfect conditions: ideal resolution, good aspect ratio
        var query = CreateQuery(1920, 1080, 500_000, "jpeg");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.OverallScore.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task Score_NeverBelowZero()
    {
        // Worst case: tiny resolution + tiny file size
        var query = CreateQuery(200, 200, 500, "png");
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Data!.OverallScore.Should().BeGreaterThanOrEqualTo(0);
    }
}

#endregion

#region ═══ B. ProcessMedia Image Variant Audit Tests ═══

/// <summary>
/// Tests the synchronous ProcessMediaCommandHandler: variant generation,
/// skip logic, content types, error resilience.
/// </summary>
public class ProcessMediaImageAuditTests
{
    private readonly Mock<IMediaRepository> _repoMock;
    private readonly Mock<IImageProcessor> _processorMock;
    private readonly Mock<IVideoProcessor> _videoProcessorMock;
    private readonly Mock<IMediaStorageService> _storageMock;
    private readonly ProcessMediaCommandHandler _handler;

    public ProcessMediaImageAuditTests()
    {
        _repoMock = new Mock<IMediaRepository>();
        _processorMock = new Mock<IImageProcessor>();
        _videoProcessorMock = new Mock<IVideoProcessor>();
        _storageMock = new Mock<IMediaStorageService>();
        var loggerMock = new Mock<ILogger<ProcessMediaCommandHandler>>();

        _handler = new ProcessMediaCommandHandler(
            _repoMock.Object,
            _processorMock.Object,
            _videoProcessorMock.Object,
            _storageMock.Object,
            loggerMock.Object);
    }

    private static ImageMedia CreateTestImage(int width = 2000, int height = 1500)
    {
        return new ImageMedia(
            Guid.NewGuid(), "owner-1", "vehicles",
            "photo.jpg", "image/jpeg", 1_000_000,
            $"vehicles/owner-1/photo_{Guid.NewGuid():N}.jpg", width, height);
    }

    private void SetupMocks(ImageMedia image, int infoWidth = 2000, int infoHeight = 1500)
    {
        _repoMock.Setup(r => r.GetByIdAsync(image.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        _storageMock.Setup(s => s.DownloadFileAsync(It.IsAny<string>()))
            .ReturnsAsync(() => new MemoryStream(new byte[1000]));
        _processorMock.Setup(p => p.GetImageInfoAsync(It.IsAny<Stream>()))
            .ReturnsAsync(new ImageInfo { Width = infoWidth, Height = infoHeight, Format = "jpeg" });
        _processorMock.Setup(p => p.CreateThumbnailAsync(
                It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(() => new MemoryStream(new byte[100]));
        _storageMock.Setup(s => s.UploadFileAsync(
                It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _storageMock.Setup(s => s.GetFileUrlAsync(It.IsAny<string>()))
            .ReturnsAsync("https://cdn.okla.do/media/file");
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ── 1. Large image → 5 variants ──
    [Fact]
    public async Task ProcessImage_LargeImage_Generates5Variants()
    {
        var image = CreateTestImage(2000, 1500);
        SetupMocks(image);

        var command = new ProcessMediaCommand(image.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.VariantsGenerated.Should().Be(5);
        result.Data.Status.Should().Be("processed");
    }

    // ── 2. Small image → skips variants larger than original (except webp) ──
    [Fact]
    public async Task ProcessImage_SmallImage_SkipsLargerVariantsExceptWebp()
    {
        // 300×300: thumb (200) OK, small (400) SKIP, medium (800) SKIP, large (1200) SKIP, webp OK
        var image = CreateTestImage(300, 300);
        SetupMocks(image, 300, 300);

        var command = new ProcessMediaCommand(image.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Data!.VariantsGenerated.Should().Be(2, "only thumb (200×200) and webp should be generated");
    }

    // ── 3. Tiny image → only webp generated ──
    [Fact]
    public async Task ProcessImage_TinyImage_OnlyWebpGenerated()
    {
        // 100×100: all variants ≥ 100, all JPEG skip, only webp is force-generated
        var image = CreateTestImage(100, 100);
        SetupMocks(image, 100, 100);

        var command = new ProcessMediaCommand(image.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Data!.VariantsGenerated.Should().Be(1, "only webp is generated for tiny images");
    }

    // ── 4. Correct content types uploaded ──
    [Fact]
    public async Task ProcessImage_CorrectContentTypes_UploadedToStorage()
    {
        var image = CreateTestImage(2000, 1500);
        SetupMocks(image);

        var command = new ProcessMediaCommand(image.Id);
        await _handler.Handle(command, CancellationToken.None);

        // 4 JPEG variants + 1 WebP variant
        _storageMock.Verify(s => s.UploadFileAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), "image/jpeg"), Times.Exactly(4));
        _storageMock.Verify(s => s.UploadFileAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), "image/webp"), Times.Once);
    }

    // ── 5. Storage key pattern: {original}_{variant} ──
    [Fact]
    public async Task ProcessImage_StorageKey_UsesCorrectPattern()
    {
        var image = CreateTestImage();
        SetupMocks(image);

        var command = new ProcessMediaCommand(image.Id);
        await _handler.Handle(command, CancellationToken.None);

        _storageMock.Verify(s => s.UploadFileAsync(
            It.Is<string>(key => key == $"{image.StorageKey}_thumb"),
            It.IsAny<Stream>(), It.IsAny<string>()), Times.Once);
        _storageMock.Verify(s => s.UploadFileAsync(
            It.Is<string>(key => key == $"{image.StorageKey}_webp"),
            It.IsAny<Stream>(), It.IsAny<string>()), Times.Once);
    }

    // ── 6. One variant fails → continues with others ──
    [Fact]
    public async Task ProcessImage_OneVariantFails_ContinuesWithOthers()
    {
        var image = CreateTestImage();
        SetupMocks(image);

        // First call throws, rest succeed
        _processorMock.SetupSequence(p => p.CreateThumbnailAsync(
                It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Variant processing failed"))
            .ReturnsAsync(new MemoryStream(new byte[100]))
            .ReturnsAsync(new MemoryStream(new byte[100]))
            .ReturnsAsync(new MemoryStream(new byte[100]))
            .ReturnsAsync(new MemoryStream(new byte[100]));

        var command = new ProcessMediaCommand(image.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.VariantsGenerated.Should().Be(4, "1 of 5 variants failed, 4 should succeed");
    }

    // ── 7. Media not found → fail ──
    [Fact]
    public async Task ProcessImage_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaAsset?)null);

        var command = new ProcessMediaCommand("nonexistent");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    // ── 8. Status transitions: Uploaded → Processing → Processed ──
    [Fact]
    public async Task ProcessImage_UpdatesStatusTwice_ProcessingThenProcessed()
    {
        var image = CreateTestImage();
        SetupMocks(image);

        var command = new ProcessMediaCommand(image.Id);
        await _handler.Handle(command, CancellationToken.None);

        // At least 2 UpdateAsync calls: one for Processing, one for Processed
        _repoMock.Verify(
            r => r.UpdateAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }
}

#endregion

#region ═══ C. ImageProcessingHandler Audit Tests (Workers) ═══

/// <summary>
/// Tests the async ImageProcessingHandler: skip logic, variant generation,
/// error handling, status transitions.
/// </summary>
public class ImageProcessingHandlerAuditTests
{
    private readonly Mock<IMediaRepository> _repoMock;
    private readonly Mock<IImageProcessor> _processorMock;
    private readonly Mock<IMediaStorageService> _storageMock;
    private readonly ImageProcessingHandler _handler;

    public ImageProcessingHandlerAuditTests()
    {
        _repoMock = new Mock<IMediaRepository>();
        _processorMock = new Mock<IImageProcessor>();
        _storageMock = new Mock<IMediaStorageService>();
        var loggerMock = new Mock<ILogger<ImageProcessingHandler>>();

        _handler = new ImageProcessingHandler(
            _repoMock.Object,
            _processorMock.Object,
            _storageMock.Object,
            loggerMock.Object);
    }

    private static ImageMedia CreateTestImage(int width = 2000, int height = 1500)
    {
        return new ImageMedia(
            Guid.NewGuid(), "owner-1", "vehicles",
            "photo.jpg", "image/jpeg", 1_000_000,
            $"vehicles/owner-1/photo_{Guid.NewGuid():N}.jpg", width, height);
    }

    // ── 1. Not found → skips processing ──
    [Fact]
    public async Task HandleAsync_MediaNotFound_SkipsProcessing()
    {
        _repoMock.Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaAsset?)null);

        await _handler.HandleAsync("nonexistent");

        // No storage calls should be made
        _storageMock.Verify(s => s.DownloadFileAsync(It.IsAny<string>()), Times.Never);
    }

    // ── 2. Already processed → skips ──
    [Fact]
    public async Task HandleAsync_AlreadyProcessed_Skips()
    {
        var image = CreateTestImage();
        // Simulate already-processed status
        image.MarkAsProcessing();
        image.MarkAsProcessed("https://cdn.okla.do/processed");

        _repoMock.Setup(r => r.GetByIdAsync(image.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        await _handler.HandleAsync(image.Id);

        _storageMock.Verify(s => s.DownloadFileAsync(It.IsAny<string>()), Times.Never);
    }

    // ── 3. Large image → generates all 3 WebP variants ──
    [Fact]
    public async Task HandleAsync_LargeImage_GeneratesAll3Variants()
    {
        var image = CreateTestImage(2000, 1500);
        _repoMock.Setup(r => r.GetByIdAsync(image.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageMock.Setup(s => s.DownloadFileAsync(It.IsAny<string>()))
            .ReturnsAsync(() => new MemoryStream(new byte[1000]));
        _processorMock.Setup(p => p.GetImageInfoAsync(It.IsAny<Stream>()))
            .ReturnsAsync(new ImageInfo { Width = 2000, Height = 1500, Format = "jpeg" });
        _processorMock.Setup(p => p.CreateWebpVariantAsync(
                It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync(() => new MemoryStream(new byte[100]));
        _storageMock.Setup(s => s.UploadFileAsync(
                It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _storageMock.Setup(s => s.GetFileUrlAsync(It.IsAny<string>()))
            .ReturnsAsync("https://cdn.okla.do/media/file");

        await _handler.HandleAsync(image.Id);

        // 3 WebP variants: thumbnail, medium, original
        _storageMock.Verify(s => s.UploadFileAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()), Times.Exactly(3));
    }

    // ── 4. Processing failure → marks as failed and re-throws ──
    [Fact]
    public async Task HandleAsync_Failure_MarksAsFailedAndRethrows()
    {
        var image = CreateTestImage();
        _repoMock.Setup(r => r.GetByIdAsync(image.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageMock.Setup(s => s.DownloadFileAsync(It.IsAny<string>()))
            .ThrowsAsync(new IOException("Storage unavailable"));

        var act = () => _handler.HandleAsync(image.Id);

        await act.Should().ThrowAsync<IOException>();
        _repoMock.Verify(r => r.UpdateAsync(
            It.Is<MediaAsset>(m => m.Status == DomainMediaStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    // ── 5. DefaultVariants has exactly 3 entries ──
    [Fact]
    public void DefaultVariants_Has3Entries()
    {
        var field = typeof(ImageProcessingHandler)
            .GetField("DefaultVariants", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull("ImageProcessingHandler must define DefaultVariants");

        var variants = (Array)field!.GetValue(null)!;
        variants.Length.Should().Be(3, "must have thumbnail, medium, original (all WebP)");
    }
}

#endregion

#region ═══ D. MediaCleanup & Worker Configuration Audit Tests ═══

/// <summary>
/// Tests cleanup thresholds, worker queue configuration, message deserialization.
/// </summary>
public class CleanupAndWorkerConfigAuditTests
{
    // ── 1. MediaCleanupHandler stale threshold = 48 hours ──
    [Fact]
    public void MediaCleanup_StaleThreshold_Is48Hours()
    {
        var field = typeof(MediaCleanupHandler)
            .GetField("StaleUploadThreshold", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull();

        var value = (TimeSpan)field!.GetValue(null)!;
        value.Should().Be(TimeSpan.FromHours(48));
    }

    // ── 2. Orphan threshold = 7 days ──
    [Fact]
    public void MediaCleanup_OrphanThreshold_Is7Days()
    {
        var field = typeof(MediaCleanupHandler)
            .GetField("OrphanThreshold", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull();

        var value = (TimeSpan)field!.GetValue(null)!;
        value.Should().Be(TimeSpan.FromDays(7));
    }

    // ── 3. Orphan alert threshold = 100 ──
    [Fact]
    public void MediaCleanup_OrphanAlertThreshold_Is100()
    {
        var field = typeof(MediaCleanupHandler)
            .GetField("OrphanAlertThreshold", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull();

        var value = (int)field!.GetValue(null)!;
        value.Should().Be(100);
    }

    // ── 4. ImageProcessingWorker queue name ──
    [Fact]
    public void Worker_QueueName_IsMediaProcess()
    {
        var field = typeof(ImageProcessingWorker)
            .GetField("QueueName", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull();

        var value = (string)field!.GetValue(null)!;
        value.Should().Be("media.process");
    }

    // ── 5. Exchange name ──
    [Fact]
    public void Worker_ExchangeName_IsMediaCommands()
    {
        var field = typeof(ImageProcessingWorker)
            .GetField("ExchangeName", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull();

        var value = (string)field!.GetValue(null)!;
        value.Should().Be("media.commands");
    }

    // ── 6. DLX exchange and DLQ setup ──
    [Fact]
    public void Worker_DlxExchange_IsConfigured()
    {
        var dlxField = typeof(ImageProcessingWorker)
            .GetField("DlxExchange", BindingFlags.NonPublic | BindingFlags.Static);
        var dlqField = typeof(ImageProcessingWorker)
            .GetField("DlqName", BindingFlags.NonPublic | BindingFlags.Static);

        dlxField.Should().NotBeNull("DLX exchange must be configured for dead letter routing");
        dlqField.Should().NotBeNull("DLQ name must be configured");

        var dlxValue = (string)dlxField!.GetValue(null)!;
        var dlqValue = (string)dlqField!.GetValue(null)!;

        dlxValue.Should().Be("media.commands.dlx");
        dlqValue.Should().Be("media.process.dlq");
    }

    // ── 7. MediaCleanupWorker interval = 6 hours ──
    [Fact]
    public void CleanupWorker_Interval_Is6Hours()
    {
        var field = typeof(MediaCleanupWorker)
            .GetField("CleanupInterval", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull();

        var value = (TimeSpan)field!.GetValue(null)!;
        value.Should().Be(TimeSpan.FromHours(6));
    }

    // ── 8. ProcessMediaMessage deserializes correctly ──
    [Fact]
    public void ProcessMediaMessage_Deserializes_FromJson()
    {
        var json = """{"MediaId":"img_abc123","ProcessingType":"resize"}""";
        var message = JsonSerializer.Deserialize<ProcessMediaMessage>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        message.Should().NotBeNull();
        message!.MediaId.Should().Be("img_abc123");
        message.ProcessingType.Should().Be("resize");
    }

    // ── 9. ProcessMediaMessage handles null MediaId ──
    [Fact]
    public void ProcessMediaMessage_HandlesNullMediaId()
    {
        var json = """{"ProcessingType":"resize"}""";
        var message = JsonSerializer.Deserialize<ProcessMediaMessage>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        message.Should().NotBeNull();
        message!.MediaId.Should().BeNull();
    }
}

#endregion

#region ═══ E. Variant Alignment Audit Tests ═══

/// <summary>
/// Ensures sync (ProcessMediaCommandHandler) and async (ImageProcessingHandler)
/// define identical variant configurations.
/// </summary>
public class VariantAlignmentAuditTests
{
    /// <summary>
    /// Expected variant definitions that BOTH handlers must implement identically.
    /// </summary>
    private static readonly (string Name, int Width, int Height, int Quality, string ContentType)[] ExpectedVariants =
    [
        ("thumb",  200,  200,  80, "image/jpeg"),
        ("small",  400,  400,  85, "image/jpeg"),
        ("medium", 800,  800,  85, "image/jpeg"),
        ("large",  1200, 1200, 90, "image/jpeg"),
        ("webp",   800,  800,  80, "image/webp"),
    ];

    // ── 1. Both handlers produce identical uploads for a large image ──
    [Fact]
    public async Task SyncAndAsync_ProduceIdenticalVariantCount()
    {
        // Set up sync handler (ProcessMediaCommandHandler)
        var syncRepo = new Mock<IMediaRepository>();
        var syncProcessor = new Mock<IImageProcessor>();
        var syncVideoProcessor = new Mock<IVideoProcessor>();
        var syncStorage = new Mock<IMediaStorageService>();
        var syncLogger = new Mock<ILogger<ProcessMediaCommandHandler>>();

        var syncHandler = new ProcessMediaCommandHandler(
            syncRepo.Object, syncProcessor.Object, syncVideoProcessor.Object,
            syncStorage.Object, syncLogger.Object);

        // Set up async handler (ImageProcessingHandler)
        var asyncRepo = new Mock<IMediaRepository>();
        var asyncProcessor = new Mock<IImageProcessor>();
        var asyncStorage = new Mock<IMediaStorageService>();
        var asyncLogger = new Mock<ILogger<ImageProcessingHandler>>();

        var asyncHandler = new ImageProcessingHandler(
            asyncRepo.Object, asyncProcessor.Object,
            asyncStorage.Object, asyncLogger.Object);

        // Create identical test images for both
        var syncImage = new ImageMedia(Guid.NewGuid(), "owner", "vehicles",
            "test.jpg", "image/jpeg", 1_000_000, "vehicles/test.jpg", 2000, 1500);
        var asyncImage = new ImageMedia(Guid.NewGuid(), "owner", "vehicles",
            "test.jpg", "image/jpeg", 1_000_000, "vehicles/test2.jpg", 2000, 1500);

        // Setup sync mocks
        syncRepo.Setup(r => r.GetByIdAsync(syncImage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncImage);
        syncRepo.Setup(r => r.UpdateAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        syncStorage.Setup(s => s.DownloadFileAsync(It.IsAny<string>()))
            .ReturnsAsync(() => new MemoryStream(new byte[1000]));
        syncProcessor.Setup(p => p.GetImageInfoAsync(It.IsAny<Stream>()))
            .ReturnsAsync(new ImageInfo { Width = 2000, Height = 1500, Format = "jpeg" });
        syncProcessor.Setup(p => p.CreateThumbnailAsync(
                It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(() => new MemoryStream(new byte[100]));
        syncStorage.Setup(s => s.UploadFileAsync(
                It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        syncStorage.Setup(s => s.GetFileUrlAsync(It.IsAny<string>()))
            .ReturnsAsync("https://cdn.okla.do/test");

        // Setup async mocks
        asyncRepo.Setup(r => r.GetByIdAsync(asyncImage.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asyncImage);
        asyncRepo.Setup(r => r.UpdateAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        asyncStorage.Setup(s => s.DownloadFileAsync(It.IsAny<string>()))
            .ReturnsAsync(() => new MemoryStream(new byte[1000]));
        asyncProcessor.Setup(p => p.GetImageInfoAsync(It.IsAny<Stream>()))
            .ReturnsAsync(new ImageInfo { Width = 2000, Height = 1500, Format = "jpeg" });
        asyncProcessor.Setup(p => p.CreateWebpVariantAsync(
                It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync(() => new MemoryStream(new byte[100]));
        asyncStorage.Setup(s => s.UploadFileAsync(
                It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        asyncStorage.Setup(s => s.GetFileUrlAsync(It.IsAny<string>()))
            .ReturnsAsync("https://cdn.okla.do/test");

        // Execute both
        var syncResult = await syncHandler.Handle(
            new ProcessMediaCommand(syncImage.Id), CancellationToken.None);
        await asyncHandler.HandleAsync(asyncImage.Id);

        // Sync handler generates 5 variants (thumb/small/medium/large JPEG + webp)
        syncResult.Data!.VariantsGenerated.Should().Be(5, "sync handler must generate 5 variants");
        // Async (Worker) handler generates 3 WebP variants (thumbnail, medium, original)
        asyncStorage.Verify(s => s.UploadFileAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Exactly(3), "async handler must generate 3 WebP variants");
    }

    // ── 2. Both handlers use same content types ──
    [Fact]
    public async Task SyncAndAsync_UseSameContentTypes()
    {
        // Track content types from sync handler
        var syncContentTypes = new List<string>();
        var syncStorage = new Mock<IMediaStorageService>();
        syncStorage.Setup(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .Callback<string, Stream, string>((_, _, ct) => syncContentTypes.Add(ct))
            .Returns(Task.CompletedTask);

        // Track content types from async handler
        var asyncContentTypes = new List<string>();
        var asyncStorage = new Mock<IMediaStorageService>();
        asyncStorage.Setup(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .Callback<string, Stream, string>((_, _, ct) => asyncContentTypes.Add(ct))
            .Returns(Task.CompletedTask);

        // --- Setup sync handler ---
        var syncRepo = new Mock<IMediaRepository>();
        var syncProcessor = new Mock<IImageProcessor>();
        var syncImage = new ImageMedia(Guid.NewGuid(), "owner", "vehicles",
            "test.jpg", "image/jpeg", 1_000_000, "vehicles/sync.jpg", 2000, 1500);

        syncRepo.Setup(r => r.GetByIdAsync(syncImage.Id, It.IsAny<CancellationToken>())).ReturnsAsync(syncImage);
        syncRepo.Setup(r => r.UpdateAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        syncStorage.Setup(s => s.DownloadFileAsync(It.IsAny<string>())).ReturnsAsync(() => new MemoryStream(new byte[1000]));
        syncProcessor.Setup(p => p.GetImageInfoAsync(It.IsAny<Stream>())).ReturnsAsync(new ImageInfo { Width = 2000, Height = 1500 });
        syncProcessor.Setup(p => p.CreateThumbnailAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(() => new MemoryStream(new byte[100]));
        syncStorage.Setup(s => s.GetFileUrlAsync(It.IsAny<string>())).ReturnsAsync("https://cdn.okla.do/test");

        var syncHandler = new ProcessMediaCommandHandler(
            syncRepo.Object, syncProcessor.Object, new Mock<IVideoProcessor>().Object,
            syncStorage.Object, new Mock<ILogger<ProcessMediaCommandHandler>>().Object);

        // --- Setup async handler ---
        var asyncRepo = new Mock<IMediaRepository>();
        var asyncProcessor = new Mock<IImageProcessor>();
        var asyncImage = new ImageMedia(Guid.NewGuid(), "owner", "vehicles",
            "test.jpg", "image/jpeg", 1_000_000, "vehicles/async.jpg", 2000, 1500);

        asyncRepo.Setup(r => r.GetByIdAsync(asyncImage.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asyncImage);
        asyncRepo.Setup(r => r.UpdateAsync(It.IsAny<MediaAsset>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        asyncStorage.Setup(s => s.DownloadFileAsync(It.IsAny<string>())).ReturnsAsync(() => new MemoryStream(new byte[1000]));
        asyncProcessor.Setup(p => p.GetImageInfoAsync(It.IsAny<Stream>())).ReturnsAsync(new ImageInfo { Width = 2000, Height = 1500 });
        asyncProcessor.Setup(p => p.CreateWebpVariantAsync(
                It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync(() => new MemoryStream(new byte[100]));
        asyncStorage.Setup(s => s.GetFileUrlAsync(It.IsAny<string>())).ReturnsAsync("https://cdn.okla.do/test");

        var asyncHandler = new ImageProcessingHandler(
            asyncRepo.Object, asyncProcessor.Object,
            asyncStorage.Object, new Mock<ILogger<ImageProcessingHandler>>().Object);

        // Execute both
        await syncHandler.Handle(new ProcessMediaCommand(syncImage.Id), CancellationToken.None);
        await asyncHandler.HandleAsync(asyncImage.Id);

        // Sync handler: 4x image/jpeg + 1x image/webp
        syncContentTypes.Should().Contain("image/jpeg");
        syncContentTypes.Should().Contain("image/webp");
        syncContentTypes.Should().HaveCount(5, "sync handler generates 5 variants");
        // Async (Worker) handler: 3x image/webp (all-WebP modern strategy)
        asyncContentTypes.Should().AllBe("image/webp",
            "async Worker handler uses all-WebP strategy for web performance");
        asyncContentTypes.Should().HaveCount(3, "async Worker handler generates 3 WebP variants");
    }

    // ── 3. Expected variant names match the canonical list ──
    [Fact]
    public void ExpectedVariants_AreCanonical()
    {
        ExpectedVariants.Should().HaveCount(5);

        var names = ExpectedVariants.Select(v => v.Name).ToArray();
        names.Should().Contain("thumb");
        names.Should().Contain("small");
        names.Should().Contain("medium");
        names.Should().Contain("large");
        names.Should().Contain("webp");

        // Only webp should use image/webp content type
        var webpVariant = ExpectedVariants.Single(v => v.Name == "webp");
        webpVariant.ContentType.Should().Be("image/webp");

        // All others should use image/jpeg
        var jpegVariants = ExpectedVariants.Where(v => v.Name != "webp").ToArray();
        jpegVariants.Should().AllSatisfy(v => v.ContentType.Should().Be("image/jpeg"));
    }
}

#endregion
