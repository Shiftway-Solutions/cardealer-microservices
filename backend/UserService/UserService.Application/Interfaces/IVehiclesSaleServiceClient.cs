using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserService.Application.Interfaces;

/// <summary>
/// Cliente HTTP para comunicarse con VehiclesSaleService
/// </summary>
public interface IVehiclesSaleServiceClient
{
    /// <summary>
    /// Obtiene los listados de vehículos de un vendedor
    /// </summary>
    Task<SellerListingsResult> GetSellerListingsAsync(
        Guid sellerId,
        int page = 1,
        int pageSize = 12,
        string? status = null);

    /// <summary>
    /// Obtiene las estadísticas de listados de un vendedor
    /// </summary>
    Task<SellerListingStatsResult?> GetSellerListingStatsAsync(Guid sellerId);
}

/// <summary>
/// Resultado de listados del vendedor desde VehiclesSaleService
/// </summary>
public class SellerListingsResult
{
    public List<VehicleListingDto> Listings { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// DTO de listado de vehículo
/// </summary>
public class VehicleListingDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "DOP";
    public string Status { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }
    public int Year { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public string? Transmission { get; set; }
    public string? FuelType { get; set; }
    public int Views { get; set; }
    public int Favorites { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Estadísticas de listados del vendedor
/// </summary>
public class SellerListingStatsResult
{
    public int TotalListings { get; set; }
    public int ActiveListings { get; set; }
    public int SoldListings { get; set; }
    public int PendingListings { get; set; }
    public int TotalViews { get; set; }
    public int TotalFavorites { get; set; }
}
