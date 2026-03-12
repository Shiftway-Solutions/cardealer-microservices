using MediatR;
using Microsoft.Extensions.Logging;
using AIProcessingService.Application.DTOs;
using AIProcessingService.Application.Features.Commands;
using AIProcessingService.Domain.Entities;
using AIProcessingService.Domain.Interfaces;
using MassTransit;
using DomainSpin360SourceType = AIProcessingService.Domain.Entities.Spin360SourceType;
using DtoSpin360SourceType = AIProcessingService.Application.DTOs.Spin360SourceType;

namespace AIProcessingService.Application.Features.Handlers;

public class ProcessImageCommandHandler : IRequestHandler<ProcessImageCommand, ProcessImageResponse>
{
    private readonly IImageProcessingJobRepository _repository;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly ILogger<ProcessImageCommandHandler> _logger;

    public ProcessImageCommandHandler(
        IImageProcessingJobRepository repository,
        ISendEndpointProvider sendEndpointProvider,
        ILogger<ProcessImageCommandHandler> logger)
    {
        _repository = repository;
        _sendEndpointProvider = sendEndpointProvider;
        _logger = logger;
    }

    public async Task<ProcessImageResponse> Handle(ProcessImageCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating image processing job for vehicle {VehicleId}", request.VehicleId);

        // Crear job en DB
        var job = new ImageProcessingJob
        {
            VehicleId = request.VehicleId,
            UserId = request.UserId,
            OriginalImageUrl = request.ImageUrl,
            Type = MapProcessingType(request.Type),
            Status = JobStatus.Queued,
            Options = MapOptions(request.Options)
        };

        await _repository.CreateAsync(job, cancellationToken);

        // Determinar la cola correcta basado en el tipo de procesamiento
        var queueName = request.Type switch
        {
            DTOs.ProcessingType.BackgroundRemoval or
            DTOs.ProcessingType.VehicleSegmentation or
            DTOs.ProcessingType.BackgroundReplacement or
            DTOs.ProcessingType.FullPipeline => "ai-processing-segmentation",
            DTOs.ProcessingType.ImageClassification or
            DTOs.ProcessingType.AngleDetection => "ai-processing-classification",
            DTOs.ProcessingType.LicensePlateMasking => "ai-processing-detection",
            DTOs.ProcessingType.ColorCorrection or
            DTOs.ProcessingType.QualityAnalysis => "ai-processing-segmentation",
            _ => "ai-processing-segmentation"
        };

        // Enviar mensaje a la cola específica de RabbitMQ
        var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{queueName}"));

        var message = new PythonWorkerMessage
        {
            job_id = job.Id.ToString(),
            vehicle_id = request.VehicleId.ToString(),
            user_id = request.UserId?.ToString() ?? "",
            image_url = request.ImageUrl,
            processing_type = request.Type.ToString(),
            options = new PythonWorkerOptions
            {
                background_id = request.Options?.BackgroundId ?? "white_studio",
                custom_background_url = request.Options?.CustomBackgroundUrl,
                mask_license_plate = request.Options?.MaskLicensePlate ?? true,
                license_plate_mask_type = request.Options?.LicensePlateMaskType ?? "blur",
                generate_shadow = request.Options?.GenerateShadow ?? true,
                auto_enhance = request.Options?.AutoEnhance ?? true,
                output_format = request.Options?.OutputFormat ?? "jpg",
                output_quality = request.Options?.OutputQuality ?? 90
            }
        };

        await endpoint.Send(message, cancellationToken);

        _logger.LogInformation("Image processing job {JobId} sent to queue {Queue}", job.Id, queueName);

        return new ProcessImageResponse(
            job.Id,
            "queued",
            "Image processing job queued successfully",
            $"/api/ai-process/status/{job.Id}",
            EstimateProcessingTime(request.Type)
        );
    }

    private static Domain.Entities.ProcessingType MapProcessingType(DTOs.ProcessingType type) =>
        type switch
        {
            DTOs.ProcessingType.BackgroundRemoval => Domain.Entities.ProcessingType.BackgroundRemoval,
            DTOs.ProcessingType.VehicleSegmentation => Domain.Entities.ProcessingType.VehicleSegmentation,
            DTOs.ProcessingType.ImageClassification => Domain.Entities.ProcessingType.ImageClassification,
            DTOs.ProcessingType.AngleDetection => Domain.Entities.ProcessingType.AngleDetection,
            DTOs.ProcessingType.LicensePlateMasking => Domain.Entities.ProcessingType.LicensePlateMasking,
            DTOs.ProcessingType.ColorCorrection => Domain.Entities.ProcessingType.ColorCorrection,
            DTOs.ProcessingType.QualityAnalysis => Domain.Entities.ProcessingType.QualityAnalysis,
            DTOs.ProcessingType.BackgroundReplacement => Domain.Entities.ProcessingType.BackgroundReplacement,
            DTOs.ProcessingType.FullPipeline => Domain.Entities.ProcessingType.FullPipeline,
            _ => Domain.Entities.ProcessingType.FullPipeline
        };

    private static ProcessingOptions MapOptions(ProcessingOptionsDto? dto)
    {
        if (dto == null) return new ProcessingOptions();

        return new ProcessingOptions
        {
            BackgroundId = dto.BackgroundId,
            CustomBackgroundUrl = dto.CustomBackgroundUrl,
            MaskLicensePlate = dto.MaskLicensePlate,
            LicensePlateMaskType = dto.LicensePlateMaskType,
            GenerateShadow = dto.GenerateShadow,
            AutoEnhance = dto.AutoEnhance,
            OutputFormat = dto.OutputFormat,
            OutputQuality = dto.OutputQuality
        };
    }

    private static int EstimateProcessingTime(DTOs.ProcessingType type) =>
        type switch
        {
            DTOs.ProcessingType.FullPipeline => 30,
            DTOs.ProcessingType.BackgroundReplacement => 15,
            DTOs.ProcessingType.VehicleSegmentation => 10,
            DTOs.ProcessingType.ImageClassification => 3,
            DTOs.ProcessingType.AngleDetection => 3,
            DTOs.ProcessingType.LicensePlateMasking => 5,
            DTOs.ProcessingType.ColorCorrection => 5,
            DTOs.ProcessingType.QualityAnalysis => 3,
            _ => 20
        };
}

public class ProcessBatchCommandHandler : IRequestHandler<ProcessBatchCommand, ProcessBatchResponse>
{
    private readonly IImageProcessingJobRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ProcessBatchCommandHandler> _logger;

    public ProcessBatchCommandHandler(
        IImageProcessingJobRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<ProcessBatchCommandHandler> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<ProcessBatchResponse> Handle(ProcessBatchCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating batch processing job for vehicle {VehicleId} with {Count} images",
            request.VehicleId, request.ImageUrls.Count);

        var jobIds = new List<Guid>();

        foreach (var imageUrl in request.ImageUrls)
        {
            var job = new ImageProcessingJob
            {
                VehicleId = request.VehicleId,
                UserId = request.UserId,
                OriginalImageUrl = imageUrl,
                Type = MapProcessingType(request.Type),
                Status = JobStatus.Queued,
                Options = MapOptions(request.Options)
            };

            await _repository.CreateAsync(job, cancellationToken);
            jobIds.Add(job.Id);

            var message = new ImageProcessingMessage(
                job.Id,
                request.VehicleId,
                imageUrl,
                request.Type,
                request.Options ?? new ProcessingOptionsDto(),
                DateTime.UtcNow
            );

            await _publishEndpoint.Publish(message, cancellationToken);
        }

        _logger.LogInformation("{Count} batch processing jobs queued for vehicle {VehicleId}",
            jobIds.Count, request.VehicleId);

        return new ProcessBatchResponse(
            jobIds,
            request.ImageUrls.Count,
            "queued",
            $"{request.ImageUrls.Count} images queued for processing",
            $"/api/ai-process/batch-status?vehicleId={request.VehicleId}",
            EstimateBatchTime(request.ImageUrls.Count, request.Type)
        );
    }

    private static Domain.Entities.ProcessingType MapProcessingType(DTOs.ProcessingType type) =>
        type switch
        {
            DTOs.ProcessingType.FullPipeline => Domain.Entities.ProcessingType.FullPipeline,
            DTOs.ProcessingType.BackgroundReplacement => Domain.Entities.ProcessingType.BackgroundReplacement,
            _ => Domain.Entities.ProcessingType.FullPipeline
        };

    private static ProcessingOptions MapOptions(ProcessingOptionsDto? dto)
    {
        if (dto == null) return new ProcessingOptions();

        return new ProcessingOptions
        {
            BackgroundId = dto.BackgroundId,
            MaskLicensePlate = dto.MaskLicensePlate,
            GenerateShadow = dto.GenerateShadow,
            AutoEnhance = dto.AutoEnhance,
            OutputFormat = dto.OutputFormat,
            OutputQuality = dto.OutputQuality
        };
    }

    private static int EstimateBatchTime(int imageCount, DTOs.ProcessingType type)
    {
        var perImage = type == DTOs.ProcessingType.FullPipeline ? 20 : 10;
        return imageCount * perImage;
    }
}

public class Generate360CommandHandler : IRequestHandler<Generate360Command, Generate360Response>
{
    private readonly ISpin360JobRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<Generate360CommandHandler> _logger;

    public Generate360CommandHandler(
        ISpin360JobRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<Generate360CommandHandler> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<Generate360Response> Handle(Generate360Command request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating 360° spin job for vehicle {VehicleId}", request.VehicleId);

        // Validar input
        if (request.SourceType == DtoSpin360SourceType.Video && string.IsNullOrEmpty(request.VideoUrl))
        {
            throw new ArgumentException("VideoUrl is required for video source type");
        }

        if (request.SourceType == DtoSpin360SourceType.Images && (request.ImageUrls == null || request.ImageUrls.Count < 6))
        {
            throw new ArgumentException("At least 6 images are required for image source type");
        }

        var job = new Spin360Job
        {
            VehicleId = request.VehicleId,
            UserId = request.UserId,
            SourceType = MapSourceType(request.SourceType),
            SourceVideoUrl = request.VideoUrl,
            SourceImageUrls = request.ImageUrls ?? new List<string>(),
            FrameCount = request.FrameCount,
            TotalFrames = request.SourceType == DtoSpin360SourceType.Video
                ? request.FrameCount
                : request.ImageUrls?.Count ?? 0,
            Status = Domain.Entities.Spin360Status.Pending,
            Options = MapOptions(request.Options)
        };

        await _repository.CreateAsync(job, cancellationToken);

        var message = new Spin360ProcessingMessage(
            job.Id,
            request.VehicleId,
            request.SourceType,
            request.VideoUrl,
            request.ImageUrls,
            request.Options ?? new Spin360OptionsDto(),
            DateTime.UtcNow
        );

        await _publishEndpoint.Publish(message, cancellationToken);

        _logger.LogInformation("360° spin job {JobId} queued for vehicle {VehicleId}", job.Id, request.VehicleId);

        var estimatedMinutes = EstimateProcessingMinutes(job.TotalFrames);

        return new Generate360Response(
            job.Id,
            "queued",
            $"360° spin generation queued with {job.TotalFrames} frames. Estimated time: {estimatedMinutes} minutes.",
            job.TotalFrames,
            $"/api/ai-process/360/status/{job.Id}",
            estimatedMinutes
        );
    }

    private static Domain.Entities.Spin360SourceType MapSourceType(DTOs.Spin360SourceType type) =>
        type switch
        {
            DTOs.Spin360SourceType.Video => Domain.Entities.Spin360SourceType.Video,
            DTOs.Spin360SourceType.Images => Domain.Entities.Spin360SourceType.Images,
            _ => Domain.Entities.Spin360SourceType.Images
        };

    private static Spin360Options MapOptions(Spin360OptionsDto? dto)
    {
        if (dto == null) return new Spin360Options();

        return new Spin360Options
        {
            TargetFrameCount = dto.TargetFrameCount,
            BackgroundId = dto.BackgroundId,
            ProcessFrames = dto.ProcessFrames,
            MaskLicensePlate = dto.MaskLicensePlate,
            GenerateShadows = dto.GenerateShadows,
            FrameWidth = dto.FrameWidth,
            FrameHeight = dto.FrameHeight
        };
    }

    private static int EstimateProcessingMinutes(int frameCount) =>
        frameCount switch
        {
            <= 12 => 2,
            <= 24 => 4,
            <= 36 => 6,
            <= 72 => 10,
            _ => 15
        };
}
// ═══════════════════════════════════════════════════════════════════════════
// UPDATE JOB STATUS HANDLER (from worker callback)
// ═══════════════════════════════════════════════════════════════════════════

public class UpdateJobStatusCommandHandler : IRequestHandler<UpdateJobStatusCommand, Unit>
{
    private readonly IImageProcessingJobRepository _repository;
    private readonly ILogger<UpdateJobStatusCommandHandler> _logger;

    public UpdateJobStatusCommandHandler(
        IImageProcessingJobRepository repository,
        ILogger<UpdateJobStatusCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateJobStatusCommand request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.JobId, cancellationToken);

        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found for callback", request.JobId);
            return Unit.Value;
        }

        if (request.Success)
        {
            job.Status = JobStatus.Completed;
            job.ProcessedImageUrl = request.ProcessedImageUrl;
            job.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("Job {JobId} completed successfully. Processed image: {Url}",
                request.JobId, request.ProcessedImageUrl);
        }
        else
        {
            job.Status = JobStatus.Failed;
            job.ErrorMessage = request.Error;
            job.CompletedAt = DateTime.UtcNow;

            _logger.LogWarning("Job {JobId} failed: {Error}", request.JobId, request.Error);
        }

        await _repository.UpdateAsync(job, cancellationToken);

        return Unit.Value;
    }
}