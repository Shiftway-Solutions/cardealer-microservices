using Microsoft.Extensions.Logging;
using Video360Service.Application.Interfaces;
using Video360Service.Domain.Entities;
using Video360Service.Domain.Enums;
using Video360Service.Domain.Interfaces;

namespace Video360Service.Infrastructure.Services;

/// <summary>
/// Orquestador principal para el procesamiento de video 360°
/// Pipeline: Video → Extracción de frames (FFmpeg local) → Eliminación de fondo (remove.bg) → Storage (S3)
/// </summary>
public class Video360Orchestrator : IVideo360Orchestrator
{
    private readonly IVideo360ProviderFactory _providerFactory;
    private readonly IVideoStorageService _storageService;
    private readonly IVideo360JobRepository _jobRepository;
    private readonly IUsageRecordRepository _usageRepository;
    private readonly IProviderConfigurationRepository _configRepository;
    private readonly IBackgroundRemovalService _backgroundRemoval;
    private readonly ILogger<Video360Orchestrator> _logger;

    public Video360Orchestrator(
        IVideo360ProviderFactory providerFactory,
        IVideoStorageService storageService,
        IVideo360JobRepository jobRepository,
        IUsageRecordRepository usageRepository,
        IProviderConfigurationRepository configRepository,
        IBackgroundRemovalService backgroundRemoval,
        ILogger<Video360Orchestrator> logger)
    {
        _providerFactory = providerFactory;
        _storageService = storageService;
        _jobRepository = jobRepository;
        _usageRepository = usageRepository;
        _configRepository = configRepository;
        _backgroundRemoval = backgroundRemoval;
        _logger = logger;
    }

    public async Task<Video360Job> ProcessVideoAsync(
        Video360Job job,
        byte[] videoBytes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Actualizar estado a Processing
            job.StartProcessing();
            await _jobRepository.UpdateAsync(job, cancellationToken);
            
            // 2. Guardar video original en storage
            var videoUrl = await _storageService.UploadVideoAsync(
                videoBytes, 
                "original.mp4",
                "video/mp4",
                cancellationToken);
            
            job.UpdateVideoUrl(videoUrl);
            await _jobRepository.UpdateAsync(job, cancellationToken);
            
            // 3. Crear opciones de extracción
            var options = CreateOptionsFromJob(job);
            
            // 4. Intentar procesar con proveedores (con fallback)
            Video360ExtractionResult? result = null;
            IVideo360Provider? successfulProvider = null;
            
            var availableProviders = await GetOrderedProvidersAsync(job.PreferredProvider, cancellationToken);
            
            foreach (var provider in availableProviders)
            {
                try
                {
                    _logger.LogInformation("Attempting extraction with {Provider}", provider.ProviderName);
                    
                    job.SetProvider(provider.ProviderType);
                    job.IncrementRetryCount();
                    await _jobRepository.UpdateAsync(job, cancellationToken);
                    
                    result = await provider.ExtractFramesAsync(videoBytes, options, cancellationToken);
                    
                    if (result.IsSuccess && result.Frames.Any())
                    {
                        successfulProvider = provider;
                        break;
                    }
                    
                    _logger.LogWarning("Provider {Provider} failed: {Error}", 
                        provider.ProviderName, result.ErrorMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception with provider {Provider}", provider.ProviderName);
                }
            }
            
            // 5. Verificar resultado
            if (result == null || !result.IsSuccess || !result.Frames.Any())
            {
                job.MarkFailed(result?.ErrorMessage ?? "All providers failed", result?.ErrorCode);
                await _jobRepository.UpdateAsync(job, cancellationToken);
                return job;
            }
            
            // 6. Guardar frames en storage
            var frames = await SaveFramesToStorageAsync(job, result.Frames, cancellationToken);
            
            // 7. Completar job
            job.CompleteProcessing(
                result.ProcessingTimeMs,
                result.CostUsd,
                result.VideoInfo?.DurationSeconds,
                result.VideoInfo?.Width,
                result.VideoInfo?.Height);
            
            foreach (var frame in frames)
            {
                job.AddFrame(frame);
            }
            
            await _jobRepository.UpdateAsync(job, cancellationToken);
            
            // 8. Registrar uso
            await RecordUsageAsync(job, successfulProvider!, cancellationToken);
            
            // 9. Actualizar contadores del proveedor
            await UpdateProviderCountersAsync(successfulProvider!.ProviderType, cancellationToken);
            
            _logger.LogInformation(
                "Video360 job {JobId} completed successfully with {FrameCount} frames using {Provider}", 
                job.Id, frames.Count, successfulProvider.ProviderName);
            
            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process video for job {JobId}", job.Id);
            
            job.MarkFailed(ex.Message, "ORCHESTRATOR_ERROR");
            await _jobRepository.UpdateAsync(job, cancellationToken);
            
            throw;
        }
    }

    public async Task<Video360Job> ProcessVideoFromUrlAsync(
        Video360Job job,
        string videoUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            job.StartProcessing();
            job.UpdateVideoUrl(videoUrl);
            await _jobRepository.UpdateAsync(job, cancellationToken);
            
            var options = CreateOptionsFromJob(job);
            
            // Intentar procesar con proveedores
            Video360ExtractionResult? result = null;
            IVideo360Provider? successfulProvider = null;
            
            var availableProviders = await GetOrderedProvidersAsync(job.PreferredProvider, cancellationToken);
            
            foreach (var provider in availableProviders)
            {
                try
                {
                    _logger.LogInformation("Attempting extraction from URL with {Provider}", provider.ProviderName);
                    
                    job.SetProvider(provider.ProviderType);
                    job.IncrementRetryCount();
                    await _jobRepository.UpdateAsync(job, cancellationToken);
                    
                    result = await provider.ExtractFramesFromUrlAsync(videoUrl, options, cancellationToken);
                    
                    if (result.IsSuccess && result.Frames.Any())
                    {
                        successfulProvider = provider;
                        break;
                    }
                    
                    _logger.LogWarning("Provider {Provider} failed: {Error}", 
                        provider.ProviderName, result.ErrorMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception with provider {Provider}", provider.ProviderName);
                }
            }
            
            if (result == null || !result.IsSuccess || !result.Frames.Any())
            {
                job.MarkFailed(result?.ErrorMessage ?? "All providers failed", result?.ErrorCode);
                await _jobRepository.UpdateAsync(job, cancellationToken);
                return job;
            }
            
            var frames = await SaveFramesToStorageAsync(job, result.Frames, cancellationToken);
            
            job.CompleteProcessing(
                result.ProcessingTimeMs,
                result.CostUsd,
                result.VideoInfo?.DurationSeconds,
                result.VideoInfo?.Width,
                result.VideoInfo?.Height);
            
            foreach (var frame in frames)
            {
                job.AddFrame(frame);
            }
            
            await _jobRepository.UpdateAsync(job, cancellationToken);
            await RecordUsageAsync(job, successfulProvider!, cancellationToken);
            await UpdateProviderCountersAsync(successfulProvider!.ProviderType, cancellationToken);
            
            _logger.LogInformation(
                "Video360 job {JobId} completed from URL with {FrameCount} frames", 
                job.Id, frames.Count);
            
            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process video from URL for job {JobId}", job.Id);
            
            job.MarkFailed(ex.Message, "ORCHESTRATOR_ERROR");
            await _jobRepository.UpdateAsync(job, cancellationToken);
            
            throw;
        }
    }

    public async Task<Video360Job> RetryJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException($"Job {jobId} not found");
        
        if (job.Status != ProcessingStatus.Failed)
        {
            throw new InvalidOperationException($"Can only retry failed jobs, current status: {job.Status}");
        }
        
        // Resetear job para reintento
        job.Status = ProcessingStatus.Pending;
        job.ErrorMessage = null;
        job.ErrorCode = null;
        job.CompletedAt = null;
        
        await _jobRepository.UpdateAsync(job, cancellationToken);
        
        // Si tenemos el video URL, reintentar
        if (!string.IsNullOrEmpty(job.VideoUrl))
        {
            return await ProcessVideoFromUrlAsync(job, job.VideoUrl, cancellationToken);
        }
        
        _logger.LogInformation("Job {JobId} reset for retry, waiting for video upload", jobId);
        return job;
    }
    
    private async Task<IEnumerable<IVideo360Provider>> GetOrderedProvidersAsync(
        Video360Provider? preferred,
        CancellationToken cancellationToken)
    {
        var providers = new List<IVideo360Provider>();
        
        // Si hay preferencia, agregar primero
        if (preferred.HasValue)
        {
            var preferredProvider = _providerFactory.GetProvider(preferred.Value);
            if (preferredProvider != null && await preferredProvider.IsAvailableAsync(cancellationToken))
            {
                providers.Add(preferredProvider);
            }
        }
        
        // Agregar resto de proveedores disponibles
        var configs = await _configRepository.GetEnabledAsync(cancellationToken);
        var configDict = configs.ToDictionary(c => c.Provider);
        
        var otherProviders = _providerFactory.GetAllProviders()
            .Where(p => !preferred.HasValue || p.ProviderType != preferred.Value)
            .Where(p => !configDict.ContainsKey(p.ProviderType) || configDict[p.ProviderType].IsEnabled)
            .OrderByDescending(p => configDict.TryGetValue(p.ProviderType, out var c) ? c.Priority : 50)
            .ThenBy(p => p.CostPerVideoUsd);
        
        foreach (var provider in otherProviders)
        {
            if (await provider.IsAvailableAsync(cancellationToken))
            {
                if (configDict.TryGetValue(provider.ProviderType, out var config) && !config.CanProcessToday())
                {
                    continue;
                }
                
                providers.Add(provider);
            }
        }
        
        return providers;
    }
    
    private async Task<List<ExtractedFrame>> SaveFramesToStorageAsync(
        Video360Job job,
        IEnumerable<ExtractedFrameData> frameData,
        CancellationToken cancellationToken)
    {
        var frames = new List<ExtractedFrame>();
        var bgRemovalAvailable = _backgroundRemoval.IsAvailable;

        if (bgRemovalAvailable)
            _logger.LogInformation("Background removal enabled for job {JobId}", job.Id);
        else
            _logger.LogInformation("Background removal skipped (no API key) for job {JobId}", job.Id);

        foreach (var data in frameData)
        {
            // Apply background removal if available
            var imageBytes = data.ImageBytes;
            var contentType = data.ContentType ?? "image/jpeg";

            if (bgRemovalAvailable)
            {
                var bgResult = await _backgroundRemoval.RemoveBackgroundAsync(imageBytes, contentType, cancellationToken);
                if (!bgResult.WasSkipped)
                {
                    imageBytes = bgResult.ImageBytes;
                    contentType = bgResult.ContentType;
                    _logger.LogDebug("Background removed for frame {Index}", data.Index);
                }
                else if (bgResult.ErrorMessage != null)
                {
                    _logger.LogWarning("BG removal skipped for frame {Index}: {Msg}", data.Index, bgResult.ErrorMessage);
                }
            }

            var extension = contentType.Split('/').LastOrDefault() ?? "jpg";
            var fileName = $"frame_{data.Index:D2}.{extension}";

            var url = await _storageService.UploadImageAsync(
                imageBytes,
                fileName,
                contentType,
                cancellationToken);

            var frame = new ExtractedFrame(
                job.Id,
                data.Index,
                data.AngleDegrees,
                data.AngleLabel,
                url,
                imageBytes.Length,
                contentType);

            frame.Width = data.Width;
            frame.Height = data.Height;
            frame.TimestampSeconds = data.TimestampSeconds;

            frames.Add(frame);
        }

        return frames;
    }
    
    private async Task RecordUsageAsync(
        Video360Job job,
        IVideo360Provider provider,
        CancellationToken cancellationToken)
    {
        var usage = new UsageRecord(
            provider.ProviderType,
            job.VehicleId ?? Guid.Empty,
            job.ProcessingTimeMs,
            job.CostUsd,
            true);
        
        usage.JobId = job.Id;
        usage.TenantId = job.TenantId;
        usage.FrameCount = job.Frames.Count;
        
        await _usageRepository.AddAsync(usage, cancellationToken);
    }
    
    private async Task UpdateProviderCountersAsync(
        Video360Provider providerType,
        CancellationToken cancellationToken)
    {
        var config = await _configRepository.GetByProviderAsync(providerType, cancellationToken);
        
        if (config != null)
        {
            config.IncrementUsage();
            await _configRepository.UpdateAsync(config, cancellationToken);
        }
    }
    
    private static Video360ExtractionOptions CreateOptionsFromJob(Video360Job job)
    {
        return new Video360ExtractionOptions
        {
            FrameCount = job.FrameCount,
            OutputFormat = job.ImageFormat,
            VideoQuality = job.VideoQuality,
            Quality = 85
        };
    }
}
