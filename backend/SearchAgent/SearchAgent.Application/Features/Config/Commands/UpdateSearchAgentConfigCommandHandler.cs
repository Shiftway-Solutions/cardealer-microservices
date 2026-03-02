using MediatR;
using Microsoft.Extensions.Logging;
using SearchAgent.Domain.Entities;
using SearchAgent.Domain.Interfaces;

namespace SearchAgent.Application.Features.Config.Commands;

public class UpdateSearchAgentConfigCommandHandler : IRequestHandler<UpdateSearchAgentConfigCommand, SearchAgentConfig>
{
    private readonly ISearchAgentConfigRepository _configRepo;
    private readonly ILogger<UpdateSearchAgentConfigCommandHandler> _logger;

    public UpdateSearchAgentConfigCommandHandler(
        ISearchAgentConfigRepository configRepo,
        ILogger<UpdateSearchAgentConfigCommandHandler> logger)
    {
        _configRepo = configRepo;
        _logger = logger;
    }

    public async Task<SearchAgentConfig> Handle(UpdateSearchAgentConfigCommand request, CancellationToken ct)
    {
        var config = await _configRepo.GetActiveConfigAsync(ct);
        var update = request.ConfigUpdate;

        // Apply partial updates
        if (update.IsEnabled.HasValue) config.IsEnabled = update.IsEnabled.Value;
        if (update.Model != null) config.Model = update.Model;
        if (update.Temperature.HasValue) config.Temperature = update.Temperature.Value;
        if (update.MaxTokens.HasValue) config.MaxTokens = update.MaxTokens.Value;
        if (update.MinResultsPerPage.HasValue) config.MinResultsPerPage = update.MinResultsPerPage.Value;
        if (update.MaxResultsPerPage.HasValue) config.MaxResultsPerPage = update.MaxResultsPerPage.Value;
        if (update.SponsoredAffinityThreshold.HasValue) config.SponsoredAffinityThreshold = update.SponsoredAffinityThreshold.Value;
        if (update.MaxSponsoredPercentage.HasValue) config.MaxSponsoredPercentage = update.MaxSponsoredPercentage.Value;
        if (update.SponsoredPositions != null) config.SponsoredPositions = update.SponsoredPositions;
        if (update.SponsoredLabel != null) config.SponsoredLabel = update.SponsoredLabel;
        if (update.PriceRelaxPercent.HasValue) config.PriceRelaxPercent = update.PriceRelaxPercent.Value;
        if (update.YearRelaxRange.HasValue) config.YearRelaxRange = update.YearRelaxRange.Value;
        if (update.MaxRelaxationLevel.HasValue) config.MaxRelaxationLevel = update.MaxRelaxationLevel.Value;
        if (update.EnableCache.HasValue) config.EnableCache = update.EnableCache.Value;
        if (update.CacheTtlSeconds.HasValue) config.CacheTtlSeconds = update.CacheTtlSeconds.Value;
        if (update.SemanticCacheThreshold.HasValue) config.SemanticCacheThreshold = update.SemanticCacheThreshold.Value;
        if (update.MaxQueriesPerMinutePerIp.HasValue) config.MaxQueriesPerMinutePerIp = update.MaxQueriesPerMinutePerIp.Value;
        if (update.AiSearchTrafficPercent.HasValue) config.AiSearchTrafficPercent = update.AiSearchTrafficPercent.Value;
        if (update.SystemPromptOverride != null) config.SystemPromptOverride = update.SystemPromptOverride;

        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = request.UpdatedBy;

        await _configRepo.UpdateConfigAsync(config, ct);

        _logger.LogInformation(
            "SearchAgent config updated by {UpdatedBy}. Enabled={IsEnabled}, Model={Model}, AiTraffic={Traffic}%",
            request.UpdatedBy, config.IsEnabled, config.Model, config.AiSearchTrafficPercent);

        return config;
    }
}
