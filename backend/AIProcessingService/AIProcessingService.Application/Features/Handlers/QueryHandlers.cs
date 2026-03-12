using MediatR;
using Microsoft.Extensions.Logging;
using AIProcessingService.Application.DTOs;
using AIProcessingService.Application.Features.Queries;
using AIProcessingService.Domain.Entities;
using AIProcessingService.Domain.Interfaces;

namespace AIProcessingService.Application.Features.Handlers;

public class GetJobStatusQueryHandler : IRequestHandler<GetJobStatusQuery, JobStatusResponse?>
{
    private readonly IImageProcessingJobRepository _repository;

    public GetJobStatusQueryHandler(IImageProcessingJobRepository repository)
    {
        _repository = repository;
    }

    public async Task<JobStatusResponse?> Handle(GetJobStatusQuery request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.JobId, cancellationToken);

        if (job == null) return null;

        return new JobStatusResponse(
            job.Id,
            job.Status.ToString().ToLower(),
            job.ErrorMessage,
            CalculateProgress(job),
            MapResult(job.Result, job.ProcessedImageUrl),
            job.CreatedAt,
            job.CompletedAt,
            job.ProcessingTimeMs
        );
    }

    private static int CalculateProgress(ImageProcessingJob job) =>
        job.Status switch
        {
            JobStatus.Pending => 0,
            JobStatus.Queued => 10,
            JobStatus.Processing => 50,
            JobStatus.Completed => 100,
            JobStatus.Failed => 100,
            JobStatus.Cancelled => 100,
            _ => 0
        };

    private static ProcessingResultDto? MapResult(ProcessingResult? result, string? processedImageUrl)
    {
        // Return result if we have either a processed URL or result data
        if (result == null && string.IsNullOrEmpty(processedImageUrl))
            return null;

        return new ProcessingResultDto(
            processedImageUrl,
            result?.ImageCategory,
            result?.CategoryConfidence ?? 0,
            result?.DetectedAngle,
            result?.AngleConfidence ?? 0,
            result?.LicensePlateDetected ?? false,
            result?.QualityScore ?? 0,
            result?.QualityIssues ?? new List<string>()
        );
    }
}

public class GetSpin360StatusQueryHandler : IRequestHandler<GetSpin360StatusQuery, Spin360StatusResponse?>
{
    private readonly ISpin360JobRepository _repository;

    public GetSpin360StatusQueryHandler(ISpin360JobRepository repository)
    {
        _repository = repository;
    }

    public async Task<Spin360StatusResponse?> Handle(GetSpin360StatusQuery request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(request.JobId, cancellationToken);

        if (job == null) return null;

        return new Spin360StatusResponse(
            job.Id,
            job.Status.ToString().ToLower(),
            job.ErrorMessage,
            job.ProgressPercent,
            job.TotalFrames,
            job.ProcessedFrames,
            MapResult(job.Result),
            job.CreatedAt,
            job.CompletedAt
        );
    }

    private static Spin360ResultDto? MapResult(Spin360Result? result)
    {
        if (result == null) return null;

        return new Spin360ResultDto(
            result.Frames.Select(f => new FrameDto(
                f.FrameNumber,
                f.Degrees,
                f.ProcessedUrl,
                f.ThumbnailUrl,
                f.IsKeyFrame
            )).ToList(),
            result.ViewerEmbedUrl,
            result.ThumbnailUrl,
            result.PreviewGifUrl,
            result.TotalFrames,
            result.DegreesPerFrame,
            result.AverageQualityScore
        );
    }
}

public class GetVehicleSpin360QueryHandler : IRequestHandler<GetVehicleSpin360Query, Spin360StatusResponse?>
{
    private readonly ISpin360JobRepository _repository;

    public GetVehicleSpin360QueryHandler(ISpin360JobRepository repository)
    {
        _repository = repository;
    }

    public async Task<Spin360StatusResponse?> Handle(GetVehicleSpin360Query request, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByVehicleIdAsync(request.VehicleId, cancellationToken);

        if (job == null) return null;

        return new Spin360StatusResponse(
            job.Id,
            job.Status.ToString().ToLower(),
            job.ErrorMessage,
            job.ProgressPercent,
            job.TotalFrames,
            job.ProcessedFrames,
            MapResult(job.Result),
            job.CreatedAt,
            job.CompletedAt
        );
    }

    private static Spin360ResultDto? MapResult(Spin360Result? result)
    {
        if (result == null) return null;

        return new Spin360ResultDto(
            result.Frames.Select(f => new FrameDto(
                f.FrameNumber,
                f.Degrees,
                f.ProcessedUrl,
                f.ThumbnailUrl,
                f.IsKeyFrame
            )).ToList(),
            result.ViewerEmbedUrl,
            result.ThumbnailUrl,
            result.PreviewGifUrl,
            result.TotalFrames,
            result.DegreesPerFrame,
            result.AverageQualityScore
        );
    }
}

public class GetAvailableBackgroundsQueryHandler : IRequestHandler<GetAvailableBackgroundsQuery, AvailableBackgroundsResponse>
{
    public Task<AvailableBackgroundsResponse> Handle(GetAvailableBackgroundsQuery request, CancellationToken cancellationToken)
    {
        var hasPremiumAccess = request.AccountType == "Dealer" && request.HasActiveSubscription;

        var backgrounds = hasPremiumAccess
            ? SystemBackgrounds.All
            : SystemBackgrounds.FreeBackgrounds;

        var backgroundDtos = backgrounds.Select(b => new BackgroundDto(
            b.Code,
            b.Name,
            b.Description,
            b.ThumbnailUrl,
            b.PreviewUrl,
            b.Type.ToString(),
            b.RequiresDealerMembership
        )).ToList();

        var defaultCode = hasPremiumAccess ? "gray_showroom" : "white_studio";

        return Task.FromResult(new AvailableBackgroundsResponse(
            backgroundDtos,
            defaultCode,
            hasPremiumAccess
        ));
    }
}

public class GetFeaturesQueryHandler : IRequestHandler<GetFeaturesQuery, FeaturesResponse>
{
    public Task<FeaturesResponse> Handle(GetFeaturesQuery request, CancellationToken cancellationToken)
    {
        var isDealer = request.AccountType == "Dealer";
        var hasPremium = isDealer && request.HasActiveSubscription;

        var freeBackgrounds = SystemBackgrounds.FreeBackgrounds.Select(b => b.Code).ToList();
        var allBackgrounds = SystemBackgrounds.All.Select(b => b.Code).ToList();

        var features = new FeaturesDto(
            BackgroundReplacement: new FeatureAccessDto(
                Available: true,
                RequiresDealerMembership: false,
                Description: "Reemplazo automático de fondo con IA",
                AvailableBackgrounds: hasPremium ? allBackgrounds : freeBackgrounds,
                DefaultBackground: hasPremium ? "gray_showroom" : "white_studio"
            ),
            Spin360: new FeatureAccessDto(
                Available: hasPremium,
                RequiresDealerMembership: true,
                Description: "Vista 360° interactiva del vehículo",
                AvailableBackgrounds: hasPremium ? allBackgrounds : null,
                DefaultBackground: hasPremium ? "gray_showroom" : null
            ),
            VideoGeneration: new FeatureAccessDto(
                Available: hasPremium,
                RequiresDealerMembership: true,
                Description: "Generación de video promocional automático",
                AvailableBackgrounds: null,
                DefaultBackground: null
            )
        );

        return Task.FromResult(new FeaturesResponse(
            request.AccountType,
            request.HasActiveSubscription,
            features
        ));
    }
}

public class GetQueueStatsQueryHandler : IRequestHandler<GetQueueStatsQuery, QueueStatsResponse>
{
    private readonly IImageProcessingJobRepository _repository;

    public GetQueueStatsQueryHandler(IImageProcessingJobRepository repository)
    {
        _repository = repository;
    }

    public async Task<QueueStatsResponse> Handle(GetQueueStatsQuery request, CancellationToken cancellationToken)
    {
        var statusCounts = await _repository.GetStatusCountsAsync(cancellationToken);
        var avgTime = await _repository.GetAverageProcessingTimeAsync(
            Domain.Entities.ProcessingType.FullPipeline, 24, cancellationToken);

        return new QueueStatsResponse(
            PendingJobs: statusCounts.GetValueOrDefault(JobStatus.Pending, 0) +
                        statusCounts.GetValueOrDefault(JobStatus.Queued, 0),
            ProcessingJobs: statusCounts.GetValueOrDefault(JobStatus.Processing, 0),
            CompletedToday: statusCounts.GetValueOrDefault(JobStatus.Completed, 0),
            FailedToday: statusCounts.GetValueOrDefault(JobStatus.Failed, 0),
            AverageProcessingTimeMs: avgTime,
            JobsByType: new Dictionary<string, int>()
        );
    }
}
