using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Domain.Interfaces;

namespace UserService.Application.UseCases.DealerModules;

/// <summary>
/// Query to get active modules for a dealer
/// </summary>
public record GetActiveDealerModulesQuery(Guid DealerId) : IRequest<List<DealerModuleDto>>;

public class GetActiveDealerModulesQueryHandler : IRequestHandler<GetActiveDealerModulesQuery, List<DealerModuleDto>>
{
    private readonly IDealerModuleRepository _dealerModuleRepository;
    private readonly ILogger<GetActiveDealerModulesQueryHandler> _logger;

    public GetActiveDealerModulesQueryHandler(
        IDealerModuleRepository dealerModuleRepository,
        ILogger<GetActiveDealerModulesQueryHandler> logger)
    {
        _dealerModuleRepository = dealerModuleRepository;
        _logger = logger;
    }

    public async Task<List<DealerModuleDto>> Handle(GetActiveDealerModulesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting active modules for dealer {DealerId}", request.DealerId);

        var dealerModules = await _dealerModuleRepository.GetActiveByDealerIdAsync(request.DealerId);

        var activeModules = dealerModules.Select(m => new DealerModuleDto
        {
            ModuleName = m.Module.Name,
            IsActive = m.IsActive,
            ActivatedAt = m.ActivatedAt,
            ExpiresAt = m.ExpiresAt
        }).ToList();

        _logger.LogInformation("Found {Count} active modules for dealer {DealerId}",
            activeModules.Count, request.DealerId);

        return activeModules;
    }
}
