namespace VehiclesSaleService.Application.Interfaces;

/// <summary>
/// Provides live exchange rates, primarily DOP/USD from the Banco Central
/// de la República Dominicana (BCRD) API.
///
/// Implementations should cache the rate and provide a fallback chain:
///   1. In-memory cached rate (refreshed every 4-6 hours)
///   2. Last successfully-fetched rate (persisted across restarts if possible)
///   3. Hardcoded fallback constant as last resort
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Gets the current DOP-per-USD exchange rate.
    /// Never throws — always returns a usable rate.
    /// </summary>
    Task<ExchangeRateResult> GetDopUsdRateAsync(CancellationToken ct = default);
}

/// <summary>
/// Result of an exchange rate lookup, including freshness metadata.
/// </summary>
public record ExchangeRateResult(
    /// <summary>The DOP-per-1-USD rate (e.g. 60.50).</summary>
    decimal Rate,
    /// <summary>When this rate was last fetched from the source.</summary>
    DateTimeOffset FetchedAt,
    /// <summary>Where this value came from.</summary>
    ExchangeRateSource Source
);

public enum ExchangeRateSource
{
    /// <summary>Live rate fetched from BCRD API.</summary>
    Live,
    /// <summary>Cached rate from a previous successful fetch.</summary>
    Cached,
    /// <summary>Hardcoded fallback — BCRD API was never reachable.</summary>
    Fallback
}
