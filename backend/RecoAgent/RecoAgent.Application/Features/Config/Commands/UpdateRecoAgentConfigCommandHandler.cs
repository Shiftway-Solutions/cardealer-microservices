using MediatR;
using Microsoft.Extensions.Logging;
using RecoAgent.Domain.Entities;
using RecoAgent.Domain.Interfaces;

namespace RecoAgent.Application.Features.Config.Commands;

public class UpdateRecoAgentConfigCommandHandler : IRequestHandler<UpdateRecoAgentConfigCommand, RecoAgentConfig>
{
    private readonly IRecoAgentConfigRepository _configRepo;
    private readonly ILogger<UpdateRecoAgentConfigCommandHandler> _logger;

    public UpdateRecoAgentConfigCommandHandler(
        IRecoAgentConfigRepository configRepo,
        ILogger<UpdateRecoAgentConfigCommandHandler> logger)
    {
        _configRepo = configRepo;
        _logger = logger;
    }

    public async Task<RecoAgentConfig> Handle(UpdateRecoAgentConfigCommand request, CancellationToken ct)
    {
        var config = await _configRepo.GetActiveConfigAsync(ct);
        var update = request.ConfigUpdate;

        // Apply partial updates
        if (update.IsEnabled.HasValue) config.IsEnabled = update.IsEnabled.Value;
        if (update.Model != null) config.Model = update.Model;
        if (update.Temperature.HasValue) config.Temperature = update.Temperature.Value;
        if (update.MaxTokens.HasValue) config.MaxTokens = update.MaxTokens.Value;
        if (update.MinRecommendations.HasValue) config.MinRecommendations = update.MinRecommendations.Value;
        if (update.MaxRecommendations.HasValue) config.MaxRecommendations = update.MaxRecommendations.Value;
        if (update.SponsoredAffinityThreshold.HasValue) config.SponsoredAffinityThreshold = update.SponsoredAffinityThreshold.Value;
        if (update.SponsoredPositions != null) config.SponsoredPositions = update.SponsoredPositions;
        if (update.SponsoredLabel != null) config.SponsoredLabel = update.SponsoredLabel;
        if (update.MaxSameBrandPercent.HasValue) config.MaxSameBrandPercent = update.MaxSameBrandPercent.Value;
        if (update.BatchRefreshIntervalHours.HasValue) config.BatchRefreshIntervalHours = update.BatchRefreshIntervalHours.Value;
        if (update.CacheTtlSeconds.HasValue) config.CacheTtlSeconds = update.CacheTtlSeconds.Value;
        if (update.RealTimeCacheTtlSeconds.HasValue) config.RealTimeCacheTtlSeconds = update.RealTimeCacheTtlSeconds.Value;

        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = request.UpdatedBy;

        await _configRepo.UpdateConfigAsync(config, ct);

        _logger.LogInformation(
            "RecoAgent config updated by {UpdatedBy}. Enabled={IsEnabled}, Model={Model}",
            request.UpdatedBy, config.IsEnabled, config.Model);

        return config;
    }
}
