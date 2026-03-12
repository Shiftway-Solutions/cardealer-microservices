namespace CarDealer.Shared.Resilience.Configuration;

/// <summary>
/// Opciones de resiliencia para llamadas HTTP
/// </summary>
public class ResilienceOptions
{
    public const string SectionName = "Resilience";

    /// <summary>
    /// Habilitar políticas de resiliencia
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Configuración de reintentos
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Configuración del circuit breaker
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Configuración de timeout
    /// </summary>
    public TimeoutOptions Timeout { get; set; } = new();

    /// <summary>
    /// Configuración de rate limiting
    /// </summary>
    public RateLimiterOptions RateLimiter { get; set; } = new();

    /// <summary>
    /// Configuración de Bulkhead Isolation (limitar concurrencia)
    /// </summary>
    public BulkheadOptions Bulkhead { get; set; } = new();

    /// <summary>
    /// Configuración de Hedging (request hedging para réplicas)
    /// </summary>
    public HedgingOptions Hedging { get; set; } = new();

    /// <summary>
    /// Configuración de Fallback (respuestas degradadas)
    /// </summary>
    public FallbackOptions Fallback { get; set; } = new();
}

/// <summary>
/// Opciones de retry exponencial
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Número máximo de reintentos
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay base entre reintentos (segundos)
    /// </summary>
    public int DelaySeconds { get; set; } = 2;

    /// <summary>
    /// Usar backoff exponencial
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Agregar jitter aleatorio para evitar thundering herd
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Códigos HTTP a reintentar (además de transient failures)
    /// </summary>
    public int[] RetryStatusCodes { get; set; } = { 408, 429, 500, 502, 503, 504 };
}

/// <summary>
/// Opciones del circuit breaker
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Ratio de fallas para abrir el circuit
    /// </summary>
    public double FailureRatio { get; set; } = 0.5; // 50%

    /// <summary>
    /// Mínimo de solicitudes antes de calcular ratio
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Ventana de muestreo (segundos)
    /// </summary>
    public int SamplingDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Duración del estado Open antes de probar Half-Open (segundos)
    /// </summary>
    public int BreakDurationSeconds { get; set; } = 30;
}

/// <summary>
/// Opciones de timeout
/// </summary>
public class TimeoutOptions
{
    /// <summary>
    /// Timeout por request individual (segundos)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout total incluyendo reintentos (segundos)
    /// </summary>
    public int TotalTimeoutSeconds { get; set; } = 120;
}

/// <summary>
/// Opciones de rate limiting para outgoing requests
/// </summary>
public class RateLimiterOptions
{
    /// <summary>
    /// Habilitar rate limiting
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Solicitudes permitidas por segundo
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Tamaño del bucket para token bucket
    /// </summary>
    public int QueueLimit { get; set; } = 10;
}
