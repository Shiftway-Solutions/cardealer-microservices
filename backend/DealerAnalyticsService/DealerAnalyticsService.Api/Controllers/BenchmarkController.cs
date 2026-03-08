using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using DealerAnalyticsService.Application.Features.Benchmark.Queries;
using DealerAnalyticsService.Application.DTOs;

namespace DealerAnalyticsService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BenchmarkController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BenchmarkController> _logger;
    
    public BenchmarkController(IMediator mediator, ILogger<BenchmarkController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    /// <summary>
    /// Obtener benchmarks del mercado para una fecha
    /// </summary>
    /// <param name="date">Fecha (opcional, default: hoy)</param>
    /// <returns>Lista de benchmarks del mercado</returns>
    [HttpGet]
    public async Task<ActionResult<List<MarketBenchmarkDto>>> GetMarketBenchmarks(
        [FromQuery] DateTime? date = null)
    {
        try
        {
            var targetDate = date ?? DateTime.UtcNow;
            
            var query = new GetMarketBenchmarkQuery(targetDate);
            var result = await _mediator.Send(query);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market benchmarks for date {Date}", date);
            return StatusCode(500, new { Message = "Error retrieving market benchmarks" });
        }
    }
    
    /// <summary>
    /// Obtener comparación simplificada para dashboard
    /// </summary>
    /// <param name="vehicleCategory">Categoría de vehículo (opcional)</param>
    /// <param name="priceRange">Rango de precio (opcional)</param>
    /// <returns>Benchmarks filtrados</returns>
    [HttpGet("comparison")]
    public async Task<ActionResult<object>> GetBenchmarkComparison(
        [FromQuery] string? vehicleCategory = null,
        [FromQuery] string? priceRange = null)
    {
        try
        {
            var query = new GetMarketBenchmarkQuery(DateTime.UtcNow);
            var benchmarks = await _mediator.Send(query);
            
            // Filtrar por categoría y rango de precio si se proporcionan
            var filtered = benchmarks.AsEnumerable();
            
            if (!string.IsNullOrEmpty(vehicleCategory))
            {
                filtered = filtered.Where(b => b.VehicleCategory.Equals(vehicleCategory, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!string.IsNullOrEmpty(priceRange))
            {
                filtered = filtered.Where(b => b.PriceRange.Equals(priceRange, StringComparison.OrdinalIgnoreCase));
            }
            
            var comparison = new
            {
                Filters = new { VehicleCategory = vehicleCategory, PriceRange = priceRange },
                Summary = new
                {
                    AveragePrice = filtered.Average(b => b.MarketAveragePrice),
                    AverageDaysOnMarket = filtered.Average(b => b.MarketAverageDaysOnMarket),
                    AverageViews = filtered.Average(b => b.MarketAverageViews),
                    AverageConversionRate = filtered.Average(b => b.MarketConversionRate)
                },
                Benchmarks = filtered.Select(b => new
                {
                    b.VehicleCategory,
                    CategoryPriceRange = b.PriceRange,
                    b.MarketAveragePrice,
                    b.MarketAverageDaysOnMarket,
                    b.MarketAverageViews,
                    b.MarketConversionRate,
                    PricePercentiles = new
                    {
                        Low = b.PricePercentile25,
                        Medium = b.PricePercentile50,
                        High = b.PricePercentile75
                    },
                    SampleSize = new
                    {
                        Dealers = b.TotalDealersInSample,
                        Vehicles = b.TotalVehiclesInSample
                    }
                }).ToList()
            };
            
            return Ok(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting benchmark comparison");
            return StatusCode(500, new { Message = "Error retrieving benchmark comparison" });
        }
    }
    
    /// <summary>
    /// Obtener categorías y rangos de precio disponibles
    /// </summary>
    /// <returns>Lista de filtros disponibles</returns>
    [HttpGet("filters")]
    public async Task<ActionResult<object>> GetAvailableFilters()
    {
        try
        {
            var query = new GetMarketBenchmarkQuery(DateTime.UtcNow);
            var benchmarks = await _mediator.Send(query);
            
            var filters = new
            {
                VehicleCategories = benchmarks.Select(b => b.VehicleCategory).Distinct().OrderBy(c => c).ToList(),
                PriceRanges = benchmarks.Select(b => b.PriceRange).Distinct().OrderBy(r => r).ToList(),
                LastUpdated = benchmarks.Any() ? benchmarks.Max(b => b.Date) : DateTime.UtcNow
            };
            
            return Ok(filters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available filters");
            return StatusCode(500, new { Message = "Error retrieving available filters" });
        }
    }
}
