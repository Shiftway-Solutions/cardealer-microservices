using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using MediaService.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;

namespace MediaService.Infrastructure.Services.Storage;

/// <summary>
/// S3/DigitalOcean Spaces storage with circuit breaker resilience.
/// All S3 SDK calls are wrapped in the "media-circuit-breaker" pipeline.
/// Degraded behavior: throws BrokenCircuitException → caller returns 503.
/// </summary>
public class S3StorageService : IMediaStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3StorageService> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    public S3StorageService(
        IConfiguration configuration,
        ILogger<S3StorageService> logger,
        ResiliencePipelineProvider<string> resiliencePipelineProvider)
    {
        _logger = logger;
        _resiliencePipeline = resiliencePipelineProvider.GetPipeline("media-circuit-breaker");

        // Bind options directly from configuration
        _options = new S3StorageOptions();
        var section = configuration.GetSection("Storage:S3");
        section.Bind(_options);

        _logger.LogInformation("S3StorageService: AccessKey={HasKey}, SecretKey={HasSecret}, Region={Region}, Bucket={Bucket}",
            !string.IsNullOrEmpty(_options.AccessKey) ? "PRESENT" : "MISSING",
            !string.IsNullOrEmpty(_options.SecretKey) ? "PRESENT" : "MISSING",
            _options.Region,
            _options.BucketName);

        if (string.IsNullOrEmpty(_options.AccessKey))
        {
            throw new InvalidOperationException($"S3 AccessKey is not configured. Check Storage:S3:AccessKey configuration.");
        }

        if (string.IsNullOrEmpty(_options.SecretKey))
        {
            throw new InvalidOperationException($"S3 SecretKey is not configured. Check Storage:S3:SecretKey configuration.");
        }

        // Parse region string to RegionEndpoint
        var region = Amazon.RegionEndpoint.GetBySystemName(_options.Region);

        // AWS SDK v3.7+ handles clock skew correction automatically via its retry mechanism.
        // See UploadFileAsync for explicit retry handling when Docker clock drift is extreme.
        var s3Config = new AmazonS3Config();

        // Support DigitalOcean Spaces and other S3-compatible providers via custom ServiceUrl
        if (!string.IsNullOrEmpty(_options.ServiceUrl))
        {
            s3Config.ServiceURL = _options.ServiceUrl;
            s3Config.ForcePathStyle = false; // DO Spaces requires virtual-hosted style
            _logger.LogInformation("S3StorageService: Using custom endpoint: {ServiceUrl}", _options.ServiceUrl);
        }
        else
        {
            s3Config.RegionEndpoint = region;
        }

        _s3Client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, s3Config);

        _logger.LogInformation("S3StorageService initialized successfully with bucket: {Bucket}, region: {Region}",
            _options.BucketName, _options.Region);
    }

    public async Task<UploadUrlResponse> GenerateUploadUrlAsync(string storageKey, string contentType, TimeSpan? expiry = null)
    {
        var expires = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(_options.PreSignedUrlExpirationMinutes));

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            Expires = expires,
            ContentType = contentType
        };

        // Enforce public-read ACL on pre-signed uploads so files uploaded
        // via pre-signed URL are publicly accessible (same as UploadFileAsync).
        request.Headers["x-amz-acl"] = "public-read";

        var uploadUrl = _s3Client.GetPreSignedURL(request);

        var response = new UploadUrlResponse
        {
            UploadUrl = uploadUrl,
            ExpiresAt = expires,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = contentType,
                ["x-amz-acl"] = "public-read"
            },
            StorageKey = storageKey
        };

        return await Task.FromResult(response);
    }

    public Task<bool> ValidateFileAsync(string contentType, long fileSize)
    {
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "video/mp4", "application/pdf" };
        var maxSize = 100 * 1024 * 1024;
        var isValid = allowedTypes.Contains(contentType) && fileSize <= maxSize;
        return Task.FromResult(isValid);
    }

    public Task<string> GenerateStorageKeyAsync(string ownerId, string? context, string fileName)
    {
        var safeFileName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "_")
            .ToLowerInvariant();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var random = Path.GetRandomFileName().Replace(".", "").Substring(0, 8);

        var key = $"{ownerId}/{context ?? "default"}/{timestamp}_{random}_{safeFileName}{extension}";
        return Task.FromResult(key);
    }

    public async Task<bool> FileExistsAsync(string storageKey)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = storageKey
            };

            await _resiliencePipeline.ExecuteAsync(async ct =>
                await _s3Client.GetObjectMetadataAsync(request, ct));
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<string> GetFileUrlAsync(string storageKey)
    {
        if (!string.IsNullOrEmpty(_options.CdnBaseUrl))
        {
            return $"{_options.CdnBaseUrl}/{storageKey}";
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Expires = DateTime.UtcNow.AddMinutes(_options.PreSignedUrlExpirationMinutes)
        };

        return await Task.FromResult(_s3Client.GetPreSignedURL(request));
    }

    public async Task UploadFileAsync(string storageKey, Stream fileStream, string contentType)
    {
        var transferUtility = new TransferUtility(_s3Client);

        var request = new TransferUtilityUploadRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            InputStream = fileStream,
            ContentType = contentType,
            AutoCloseStream = false,
            CannedACL = S3CannedACL.PublicRead // Ensure all uploaded objects are publicly readable
        };

        try
        {
            await _resiliencePipeline.ExecuteAsync(async _ =>
                await transferUtility.UploadAsync(request));
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "RequestTimeTooSkewed")
        {
            // Docker VM clock drift on macOS after sleep/wake cycles can cause AWS to reject
            // requests (>5 min diff). The SDK should auto-correct on the retry attempt since
            // it captures the server's Date header from the error response and adjusts.
            _logger.LogWarning(
                "S3 RequestTimeTooSkewed on first attempt for {StorageKey}. " +
                "Retrying — SDK will apply corrected timestamp. " +
                "If this recurs, restart the MediaService container to resync Docker VM clock.",
                storageKey);

            fileStream.Position = 0; // Reset stream before retry
            await _resiliencePipeline.ExecuteAsync(async _ =>
                await transferUtility.UploadAsync(request));
        }
    }

    public async Task<Stream> DownloadFileAsync(string storageKey)
    {
        var request = new GetObjectRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey
        };

        var response = await _resiliencePipeline.ExecuteAsync(async ct =>
            await _s3Client.GetObjectAsync(request, ct));
        return response.ResponseStream;
    }

    public async Task DeleteFileAsync(string storageKey)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey
        };

        await _resiliencePipeline.ExecuteAsync(async ct =>
            await _s3Client.DeleteObjectAsync(request, ct));
    }

    public async Task CopyFileAsync(string sourceKey, string destinationKey)
    {
        var request = new CopyObjectRequest
        {
            SourceBucket = _options.BucketName,
            SourceKey = sourceKey,
            DestinationBucket = _options.BucketName,
            DestinationKey = destinationKey,
            CannedACL = S3CannedACL.PublicRead // Preserve public-read on copied objects
        };

        await _resiliencePipeline.ExecuteAsync(async ct =>
            await _s3Client.CopyObjectAsync(request, ct));
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await _s3Client.ListBucketsAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 storage health check failed");
            return false;
        }
    }
}
