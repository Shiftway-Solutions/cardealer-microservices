using Microsoft.EntityFrameworkCore;
using DealerAnalyticsService.Domain.Entities;
using DealerAnalyticsService.Domain.Interfaces;

namespace DealerAnalyticsService.Infrastructure.Persistence.Repositories;

public class MarketBenchmarkRepository : IMarketBenchmarkRepository
{
    private readonly DealerAnalyticsDbContext _context;

    public MarketBenchmarkRepository(DealerAnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task<MarketBenchmark?> GetBenchmarkAsync(string vehicleCategory, string priceRange, DateTime date)
    {
        return await _context.MarketBenchmarks
            .FirstOrDefaultAsync(x => x.VehicleCategory == vehicleCategory &&
                                    x.PriceRange == priceRange &&
                                    x.Date.Date == date.Date);
    }

    public async Task<IEnumerable<MarketBenchmark>> GetBenchmarksAsync(DateTime date)
    {
        return await _context.MarketBenchmarks
            .Where(x => x.Date.Date == date.Date)
            .OrderBy(x => x.VehicleCategory)
            .ThenBy(x => x.PriceRange)
            .ToListAsync();
    }

    public async Task<MarketBenchmark> CreateOrUpdateBenchmarkAsync(MarketBenchmark benchmark)
    {
        var existing = await GetBenchmarkAsync(benchmark.VehicleCategory, benchmark.PriceRange, benchmark.Date);

        if (existing == null)
        {
            benchmark.Id = Guid.NewGuid();
            benchmark.CreatedAt = DateTime.UtcNow;
            benchmark.UpdatedAt = DateTime.UtcNow;

            _context.MarketBenchmarks.Add(benchmark);
        }
        else
        {
            existing.MarketAveragePrice = benchmark.MarketAveragePrice;
            existing.MarketAverageDaysOnMarket = benchmark.MarketAverageDaysOnMarket;
            existing.MarketAverageViews = benchmark.MarketAverageViews;
            existing.MarketConversionRate = benchmark.MarketConversionRate;
            existing.PricePercentile25 = benchmark.PricePercentile25;
            existing.PricePercentile50 = benchmark.PricePercentile50;
            existing.PricePercentile75 = benchmark.PricePercentile75;
            existing.TotalDealersInSample = benchmark.TotalDealersInSample;
            existing.TotalVehiclesInSample = benchmark.TotalVehiclesInSample;
            existing.UpdatedAt = DateTime.UtcNow;

            benchmark = existing;
        }

        await _context.SaveChangesAsync();
        return benchmark;
    }

    public async Task DeleteBenchmarkAsync(string vehicleCategory, string priceRange, DateTime date)
    {
        var benchmark = await GetBenchmarkAsync(vehicleCategory, priceRange, date);
        if (benchmark != null)
        {
            _context.MarketBenchmarks.Remove(benchmark);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<decimal> CompareDealerToBenchmarkAsync(Guid dealerId, string metric, DateTime date)
    {
        // Este método compara las métricas del dealer con el benchmarks del mercado
        // Por simplicidad, devolvemos un valor simulado
        // En un sistema real, esto requeriría lógica más compleja

        var random = new Random();

        return metric switch
        {
            "price" => (decimal)(random.NextDouble() * 30 - 15), // -15% a +15%
            "days_on_market" => (decimal)(random.NextDouble() * 40 - 20), // -20% a +20%
            "conversion_rate" => (decimal)(random.NextDouble() * 50 - 25), // -25% a +25%
            "views" => (decimal)(random.NextDouble() * 60 - 30), // -30% a +30%
            _ => 0
        };
    }

    public async Task RecalculateBenchmarksAsync(DateTime date)
    {
        // En un sistema real, esto calcularía los benchmarks basado en todos los dealers
        // Por ahora, creamos datos de ejemplo

        var categories = new[] { "SUV", "Sedan", "Camioneta", "Deportivo", "Compacto" };
        var priceRanges = new[] { "0-1M", "1-2M", "2-3M", "3M+" };

        var random = new Random();

        foreach (var category in categories)
        {
            foreach (var priceRange in priceRanges)
            {
                var benchmark = new MarketBenchmark
                {
                    VehicleCategory = category,
                    PriceRange = priceRange,
                    Date = date,
                    MarketAveragePrice = random.Next(500000, 5000000),
                    MarketAverageDaysOnMarket = (decimal)(random.NextDouble() * 60 + 30), // 30-90 días
                    MarketAverageViews = (decimal)(random.NextDouble() * 500 + 100), // 100-600 vistas
                    MarketConversionRate = (decimal)(random.NextDouble() * 5 + 1), // 1-6%
                    PricePercentile25 = random.Next(300000, 1500000),
                    PricePercentile50 = random.Next(800000, 3000000),
                    PricePercentile75 = random.Next(1500000, 5000000),
                    TotalDealersInSample = random.Next(20, 100),
                    TotalVehiclesInSample = random.Next(100, 1000)
                };

                await CreateOrUpdateBenchmarkAsync(benchmark);
            }
        }
    }
}
