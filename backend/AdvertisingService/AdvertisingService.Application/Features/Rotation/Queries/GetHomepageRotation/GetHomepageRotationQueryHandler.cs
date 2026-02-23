using AdvertisingService.Application.Clients;
using AdvertisingService.Application.DTOs;
using AdvertisingService.Application.Interfaces;
using AdvertisingService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdvertisingService.Application.Features.Rotation.Queries.GetHomepageRotation;

public class GetHomepageRotationQueryHandler : IRequestHandler<GetHomepageRotationQuery, HomepageRotationDto?>
{
    private readonly IHomepageRotationCacheService _cacheService;
    private readonly VehicleServiceClient _vehicleClient;
    private readonly ILogger<GetHomepageRotationQueryHandler> _logger;

    public GetHomepageRotationQueryHandler(
        IHomepageRotationCacheService cacheService,
        VehicleServiceClient vehicleClient,
        ILogger<GetHomepageRotationQueryHandler> logger)
    {
        _cacheService = cacheService;
        _vehicleClient = vehicleClient;
        _logger = logger;
    }

    public async Task<HomepageRotationDto?> Handle(GetHomepageRotationQuery request, CancellationToken ct)
    {
        var result = await _cacheService.GetRotationAsync(request.Section, ct);
        if (result == null) return null;

        var isPremiumSection = request.Section == AdPlacementType.PremiumSpot;

        // E-002: Enrich all rotation slots in parallel (each call has its own 2-s timeout
        // inside VehicleServiceClient.GetVehicleBasicInfoAsync, so we just fire all together).
        var enrichmentTasks = result.Vehicles
            .Select(v => _vehicleClient.GetVehicleBasicInfoAsync(v.VehicleId, ct))
            .ToArray();

        VehicleBasicInfo?[] vehicleDetails;
        try
        {
            vehicleDetails = await Task.WhenAll(enrichmentTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error enriching rotation vehicles for section {Section}", request.Section);
            vehicleDetails = new VehicleBasicInfo?[result.Vehicles.Count];
        }

        var items = result.Vehicles
            .Select((v, i) =>
            {
                var d = vehicleDetails.Length > i ? vehicleDetails[i] : null;

                // Determine isFeatured/isPremium:
                // - If vehicle DB already has flags set (via campaign sync), use those.
                // - Otherwise fall back to the rotation section context.
                var isFeatured = d?.IsFeatured ?? !isPremiumSection;
                var isPremium  = d?.IsPremium  ?? isPremiumSection;

                return new RotatedVehicleDto(
                    CampaignId: v.CampaignId,
                    VehicleId:  v.VehicleId,
                    OwnerId:    v.OwnerId,
                    OwnerType:  v.OwnerType,
                    Position:   v.Position,
                    Score:      v.Score,
                    Title:      d?.Title,
                    Slug:       d?.Slug,
                    ImageUrl:   d?.PrimaryImageUrl,
                    Price:      d?.Price ?? 0m,
                    Currency:   d?.Currency ?? "DOP",
                    Location:   d?.Location,
                    IsFeatured: isFeatured,
                    IsPremium:  isPremium
                );
            })
            .ToList();

        return new HomepageRotationDto(
            result.Section,
            items,
            result.AlgorithmUsed,
            result.GeneratedAt
        );
    }
}

