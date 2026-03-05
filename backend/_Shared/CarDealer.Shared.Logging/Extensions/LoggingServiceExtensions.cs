using CarDealer.Shared.Logging.Middleware;
using CarDealer.Shared.Logging.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarDealer.Shared.Logging.Extensions;

/// <summary>
/// Extension methods for adding logging services to DI
/// </summary>
public static class LoggingServiceExtensions
{
    /// <summary>
    /// Adds logging configuration options to the service collection
    /// </summary>
    public static IServiceCollection AddStandardLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var options = new LoggingOptions { ServiceName = serviceName };
        configuration.GetSection(LoggingOptions.SectionName).Bind(options);

        services.Configure<LoggingOptions>(opt =>
        {
            opt.ServiceName = serviceName;
            configuration.GetSection(LoggingOptions.SectionName).Bind(opt);
        });

        return services;
    }

    /// <summary>
    /// Uses the request logging middleware that enriches logs with request context
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }

    /// <summary>
    /// Uses Serilog request logging with standard enriched configuration
    /// </summary>
    /// <summary>
    /// Headers that MUST NOT appear in logs (token leakage prevention — CWE-532).
    /// </summary>
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-CSRF-Token"
    };

    public static IApplicationBuilder UseStandardSerilogRequestLogging(this IApplicationBuilder app)
    {
        return Serilog.SerilogApplicationBuilderExtensions.UseSerilogRequestLogging(app, options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("QueryString", httpContext.Request.QueryString.Value);
                diagnosticContext.Set("ContentType", httpContext.Request.ContentType ?? "N/A");

                var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
                diagnosticContext.Set("UserAgent", userAgent);

                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? "N/A");
                }

                // Security (CWE-532): Never log sensitive headers (Authorization, Cookie, etc.)
                // Only log safe, non-sensitive headers for diagnostics.
                // DO NOT add a "RequestHeaders" property — it would leak tokens.
            };

            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        });
    }
}
