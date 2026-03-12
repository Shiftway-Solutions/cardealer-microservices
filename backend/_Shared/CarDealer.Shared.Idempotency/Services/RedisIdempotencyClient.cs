using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CarDealer.Shared.Idempotency.Interfaces;
using CarDealer.Shared.Idempotency.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CarDealer.Shared.Idempotency.Services;

/// <summary>
/// Redis-based idempotency client
/// </summary>
public class RedisIdempotencyClient : IIdempotencyClient
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IdempotencyOptions _options;
    private readonly ILogger<RedisIdempotencyClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisIdempotencyClient(
        IConnectionMultiplexer redis,
        IOptions<IdempotencyOptions> options,
        ILogger<RedisIdempotencyClient> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<IdempotencyCheckResult> CheckAsync(
        string key,
        string? requestHash = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(key);
            var db = _redis.GetDatabase();
            var data = await db.StringGetAsync(cacheKey);

            if (data.IsNullOrEmpty)
            {
                return IdempotencyCheckResult.NotFound();
            }

            var record = JsonSerializer.Deserialize<IdempotencyRecord>(data!, _jsonOptions);

            if (record == null)
            {
                return IdempotencyCheckResult.NotFound();
            }

            // Check for conflict (different request body with same key)
            if (_options.ValidateRequestHash &&
                !string.IsNullOrEmpty(requestHash) &&
                !string.IsNullOrEmpty(record.RequestHash) &&
                record.RequestHash != requestHash)
            {
                _logger.LogWarning(
                    "Idempotency conflict detected: Key={Key} has different request body",
                    key);
                return IdempotencyCheckResult.Conflict(record,
                    "Request body differs from the original request with this idempotency key");
            }

            // Check if still processing but timeout exceeded
            if (record.Status == IdempotencyStatus.Processing)
            {
                var processingTime = DateTime.UtcNow - record.CreatedAt;
                if (processingTime.TotalSeconds > _options.ProcessingTimeoutSeconds)
                {
                    _logger.LogWarning(
                        "Idempotency record timeout: Key={Key} has been processing for {Seconds}s",
                        key, processingTime.TotalSeconds);

                    // Delete stale record and allow retry
                    await DeleteAsync(key, cancellationToken);
                    return IdempotencyCheckResult.NotFound();
                }

                _logger.LogInformation("Request with key {Key} is still processing", key);
                return IdempotencyCheckResult.Processing(record);
            }

            _logger.LogInformation("Returning cached response for idempotency key {Key}", key);
            return IdempotencyCheckResult.Completed(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking idempotency key {Key}", key);
            // On error, allow the request to proceed (fail-open)
            return IdempotencyCheckResult.NotFound();
        }
    }

    public async Task<bool> StartProcessingAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(record.Key);
            var db = _redis.GetDatabase();

            record.Status = IdempotencyStatus.Processing;
            record.CreatedAt = DateTime.UtcNow;
            record.ExpiresAt = DateTime.UtcNow.AddSeconds(_options.DefaultTtlSeconds);

            var data = JsonSerializer.Serialize(record, _jsonOptions);
            var ttl = TimeSpan.FromSeconds(_options.DefaultTtlSeconds);

            // Use SetNX (set if not exists) to prevent race conditions
            var success = await db.StringSetAsync(cacheKey, data, ttl, When.NotExists);

            if (success)
            {
                _logger.LogDebug("Started processing for idempotency key {Key}", record.Key);
            }
            else
            {
                _logger.LogWarning("Failed to start processing - key already exists: {Key}", record.Key);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting processing for key {Key}", record.Key);
            return false;
        }
    }

    public async Task<bool> CompleteAsync(
        string key,
        int statusCode,
        string responseBody,
        string contentType = "application/json",
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(key);
            var db = _redis.GetDatabase();
            var data = await db.StringGetAsync(cacheKey);

            if (data.IsNullOrEmpty)
            {
                _logger.LogWarning("Cannot complete: record not found for key {Key}", key);
                return false;
            }

            var record = JsonSerializer.Deserialize<IdempotencyRecord>(data!, _jsonOptions);

            if (record == null)
            {
                return false;
            }

            record.Status = IdempotencyStatus.Completed;
            record.ResponseStatusCode = statusCode;
            record.ResponseBody = responseBody;
            record.ResponseContentType = contentType;

            if (headers != null)
            {
                record.ResponseHeaders = headers;
            }

            var updatedData = JsonSerializer.Serialize(record, _jsonOptions);
            var remainingTtl = record.ExpiresAt - DateTime.UtcNow;

            if (remainingTtl <= TimeSpan.Zero)
            {
                remainingTtl = TimeSpan.FromSeconds(_options.DefaultTtlSeconds);
            }

            await db.StringSetAsync(cacheKey, updatedData, remainingTtl);

            _logger.LogDebug("Completed idempotency record for key {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing record for key {Key}", key);
            return false;
        }
    }

    public async Task<bool> FailAsync(
        string key,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(key);
            var db = _redis.GetDatabase();
            var data = await db.StringGetAsync(cacheKey);

            if (data.IsNullOrEmpty)
            {
                return false;
            }

            var record = JsonSerializer.Deserialize<IdempotencyRecord>(data!, _jsonOptions);

            if (record == null)
            {
                return false;
            }

            record.Status = IdempotencyStatus.Failed;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                record.Metadata["error"] = errorMessage;
            }

            var updatedData = JsonSerializer.Serialize(record, _jsonOptions);

            // Failed records expire faster (5 minutes) to allow retry
            await db.StringSetAsync(cacheKey, updatedData, TimeSpan.FromMinutes(5));

            _logger.LogDebug("Marked idempotency record as failed for key {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking record as failed for key {Key}", key);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(key);
            var db = _redis.GetDatabase();
            return await db.KeyDeleteAsync(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting idempotency key {Key}", key);
            return false;
        }
    }

    public string GenerateRequestHash(string requestBody)
    {
        if (string.IsNullOrEmpty(requestBody))
        {
            return string.Empty;
        }

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(requestBody);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private string GetCacheKey(string key) => $"{_options.KeyPrefix}:{key}";
}
