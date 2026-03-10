using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CarDealer.Contracts.Events.Dealer;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Interfaces.Repositories;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Consumes DealerCreatedEvent (published when admin approves a dealer) and schedules
/// a 7-day onboarding report email persisted in the database. When the delay elapses,
/// fetches analytics data from DealerAnalyticsService, renders the report email,
/// and sends it to the dealer.
///
/// Flow:
///   1. DealerCreatedEvent received → persist schedule in DB via ScheduledNotification
///   2. Background loop checks every 5 minutes for due reports
///   3. For due reports → GET /api/dealer-analytics/reports/{dealerId}/weekly
///   4. Render EmailTemplates/DealerOnboardingReport.html template with analytics data
///   5. Send email to dealer owner
///   6. Send WhatsApp summary (if phone available)
///   7. Send in-app notification
///   8. Admin alert for monitoring
///
/// Persistence: Schedules survive pod restarts via PostgreSQL-backed ScheduledNotification.
/// Idempotency: Uses NotificationLog to prevent duplicate sends.
/// Retry: Failed reports are rescheduled up to 3 times with exponential backoff.
/// </summary>
public class DealerOnboardingReportConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DealerOnboardingReportConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "notificationservice.dealer.onboarding_report";
    private const string RoutingKey = "dealer.created";

    // How many days after activation to send the report
    private static readonly TimeSpan OnboardingReportDelay = TimeSpan.FromDays(7);

    // How often to check for due reports
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    // Maximum retry attempts for failed report sends
    private const int MaxRetryAttempts = 3;

    // Template file path (relative to TemplatesPath root)
    private const string TemplateFile = "EmailTemplates/DealerOnboardingReport.html";

    // Service-to-service internal API key header
    private const string InternalApiKeyHeader = "X-Internal-Service-Key";

    // ── Prometheus Metrics ────────────────────────────────────
    private static readonly Meter Meter = new("NotificationService.DealerOnboarding", "1.0");
    private static readonly Counter<long> ReportsScheduled = Meter.CreateCounter<long>(
        "onboarding_reports_scheduled_total", "Total onboarding reports scheduled");
    private static readonly Counter<long> ReportsSent = Meter.CreateCounter<long>(
        "onboarding_reports_sent_total", "Total onboarding reports sent successfully");
    private static readonly Counter<long> ReportsFailed = Meter.CreateCounter<long>(
        "onboarding_reports_failed_total", "Total onboarding reports that failed to send");

    public DealerOnboardingReportConsumer(
        IServiceProvider serviceProvider,
        ILogger<DealerOnboardingReportConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitMQEnabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        if (!rabbitMQEnabled)
        {
            _logger.LogInformation("RabbitMQ is disabled. DealerOnboardingReportConsumer will not start.");
            return;
        }

        try
        {
            InitializeRabbitMQ();

            if (_channel == null)
            {
                _logger.LogWarning("RabbitMQ channel is null. DealerOnboardingReportConsumer will not start.");
                return;
            }

            // Start RabbitMQ consumer for dealer.created events
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var dealerEvent = JsonSerializer.Deserialize<DealerCreatedEvent>(message, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (dealerEvent != null)
                    {
                        _logger.LogInformation(
                            "Received DealerCreatedEvent: DealerId={DealerId}, OwnerUserId={OwnerUserId}, ApprovedAt={ApprovedAt}",
                            dealerEvent.DealerId, dealerEvent.OwnerUserId, dealerEvent.ApprovedAt);

                        // ── Persist schedule in DB (survives pod restarts) ────────
                        await PersistOnboardingScheduleAsync(dealerEvent, stoppingToken);

                        ReportsScheduled.Add(1);
                        _logger.LogInformation(
                            "Persisted 7-day onboarding report schedule for DealerId={DealerId}, DueAt={DueAt}",
                            dealerEvent.DealerId, dealerEvent.ApprovedAt.Add(OnboardingReportDelay));

                        // Create in-app notification about upcoming report
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var userNotifService = scope.ServiceProvider.GetService<IUserNotificationService>();
                            if (userNotifService != null && dealerEvent.OwnerUserId.HasValue)
                            {
                                await userNotifService.CreateAsync(
                                    userId: dealerEvent.OwnerUserId.Value,
                                    type: "dealer_activated",
                                    title: "🎉 ¡Bienvenido a OKLA!",
                                    message: "Tu cuenta de dealer ha sido aprobada. En 7 días recibirás tu primer reporte de rendimiento.",
                                    icon: "🎉",
                                    link: "/dashboard",
                                    cancellationToken: stoppingToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to create welcome in-app notification. Non-critical.");
                        }

                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize DealerCreatedEvent");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing DealerCreatedEvent for onboarding report");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("DealerOnboardingReportConsumer started listening on queue: {Queue}", QueueName);

            // Background loop to check for due reports from DB
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDueReportsFromDbAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DealerOnboardingReportConsumer check loop");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in DealerOnboardingReportConsumer");
        }
    }

    /// <summary>
    /// Persists the onboarding report schedule to the database via OnboardingReportSchedule.
    /// This ensures schedules survive pod restarts.
    /// Uses DealerId as idempotency key to prevent duplicates.
    /// </summary>
    private async Task PersistOnboardingScheduleAsync(DealerCreatedEvent dealerEvent, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOnboardingReportScheduleRepository>();

        // Idempotency check: don't schedule if already exists for this dealer
        var idempotencyKey = $"onboarding_report:{dealerEvent.DealerId}";
        var existing = await repo.GetByIdempotencyKeyAsync(idempotencyKey);
        if (existing != null)
        {
            _logger.LogInformation(
                "Onboarding report already scheduled/sent for DealerId={DealerId}. Skipping duplicate.",
                dealerEvent.DealerId);
            return;
        }

        var schedule = Domain.Entities.OnboardingReportSchedule.Create(
            dealerEvent.DealerId,
            dealerEvent.OwnerUserId,
            dealerEvent.ApprovedAt,
            OnboardingReportDelay);

        await repo.AddAsync(schedule);
    }

    /// <summary>
    /// Queries the database for due onboarding reports and processes them.
    /// Handles retry with exponential backoff for failed reports.
    /// </summary>
    private async Task ProcessDueReportsFromDbAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOnboardingReportScheduleRepository>();

        // Get all scheduled onboarding reports that are now due
        var dueReports = await repo.GetDueReportsAsync(DateTime.UtcNow);

        foreach (var schedule in dueReports)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // Mark as processing to prevent other pods from picking it up
                schedule.MarkAsProcessing();
                await repo.UpdateAsync(schedule);

                await SendOnboardingReportAsync(schedule.DealerId, schedule.OwnerUserId, schedule.ApprovedAt, ct);

                // Mark as sent successfully
                schedule.MarkAsSent();
                await repo.UpdateAsync(schedule);

                ReportsSent.Add(1);
                _logger.LogInformation(
                    "✅ Onboarding report sent and persisted for DealerId={DealerId}", schedule.DealerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send onboarding report ScheduleId={ScheduleId}. Retry {RetryCount}/{MaxRetries}",
                    schedule.Id, schedule.RetryCount + 1, schedule.MaxRetries);

                schedule.MarkAsFailed(ex.Message);
                await repo.UpdateAsync(schedule);

                if (!schedule.CanRetry())
                {
                    ReportsFailed.Add(1);
                    _logger.LogError(
                        "❌ Onboarding report permanently failed after {MaxRetries} retries for DealerId={DealerId}",
                        schedule.MaxRetries, schedule.DealerId);
                }
                else
                {
                    _logger.LogWarning(
                        "Rescheduled onboarding report ScheduleId={ScheduleId} for {NextAttempt}",
                        schedule.Id, schedule.DueAt);
                }
            }
        }
    }

    private async Task SendOnboardingReportAsync(Guid dealerId, Guid? ownerUserId, DateTime approvedAt, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();

        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var templateEngine = scope.ServiceProvider.GetRequiredService<ITemplateEngine>();
        var userNotifService = scope.ServiceProvider.GetService<IUserNotificationService>();
        var adminAlertService = scope.ServiceProvider.GetService<IAdminAlertService>();

        _logger.LogInformation("Generating 7-day onboarding report for DealerId={DealerId}", dealerId);

        // ── 1. Fetch dealer profile (email + name) from UserService ───────────
        string? dealerEmail = null;
        string? dealerName = null;
        string? dealerPhone = null;

        try
        {
            var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();
            if (httpClientFactory != null)
            {
                var userClient = httpClientFactory.CreateClient("UserService");

                // If we have ownerUserId, resolve their profile
                if (ownerUserId.HasValue)
                {
                    var response = await userClient.GetAsync($"/api/users/{ownerUserId.Value}/profile", ct);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(ct);
                        var profile = JsonSerializer.Deserialize<JsonElement>(json);
                        dealerEmail = profile.TryGetProperty("email", out var e) ? e.GetString() : null;
                        dealerName = profile.TryGetProperty("fullName", out var n) ? n.GetString() : null;
                        dealerPhone = profile.TryGetProperty("phoneNumber", out var p) ? p.GetString() : null;
                    }
                }

                // Fallback: try dealer endpoint
                if (string.IsNullOrWhiteSpace(dealerEmail))
                {
                    var dealerResp = await userClient.GetAsync($"/api/dealers/{dealerId}", ct);
                    if (dealerResp.IsSuccessStatusCode)
                    {
                        var json = await dealerResp.Content.ReadAsStringAsync(ct);
                        var dealer = JsonSerializer.Deserialize<JsonElement>(json);
                        dealerEmail = dealer.TryGetProperty("email", out var de) ? de.GetString() : null;
                        dealerName = dealer.TryGetProperty("companyName", out var dn) ? dn.GetString() : dealerName;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve dealer info for DealerId={DealerId}", dealerId);
        }

        if (string.IsNullOrWhiteSpace(dealerEmail))
        {
            _logger.LogWarning("No email found for DealerId={DealerId}. Cannot send onboarding report.", dealerId);
            return;
        }

        // ── 2. Fetch 7-day analytics from DealerAnalyticsService ───────────────
        int totalViews = 0, totalLeads = 0;
        string topVehicleTitle = "—", topVehicleViews = "0", topVehicleContacts = "0";
        double viewsPerListing = 0, contactsPerListing = 0, contactRate = 0;
        double marketAvgViews = 0, marketAvgContacts = 0, marketAvgContactRate = 0;

        try
        {
            var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();
            if (httpClientFactory != null)
            {
                var analyticsClient = httpClientFactory.CreateClient("DealerAnalyticsService");
                var weekStart = approvedAt.Date.ToString("yyyy-MM-dd");

                // Add internal service-to-service authentication header
                var internalKey = _configuration["Services:InternalApiKey"] ?? "internal-service-key";
                analyticsClient.DefaultRequestHeaders.Remove(InternalApiKeyHeader);
                analyticsClient.DefaultRequestHeaders.Add(InternalApiKeyHeader, internalKey);

                var response = await analyticsClient.GetAsync(
                    $"/api/dealer-analytics/reports/{dealerId}/weekly?weekStartDate={weekStart}", ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var report = JsonSerializer.Deserialize<JsonElement>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Extract KPIs — handle both camelCase and PascalCase property names
                    if (report.TryGetProperty("kpis", out var kpis) || report.TryGetProperty("Kpis", out kpis))
                    {
                        totalViews = TryGetInt(kpis, "totalViews", "TotalViews");
                        totalLeads = TryGetInt(kpis, "totalContacts", "TotalContacts", "totalLeads", "TotalLeads");
                        viewsPerListing = TryGetDouble(kpis, "viewsPerListing", "ViewsPerListing", "avgViewsPerListing", "AvgViewsPerListing");
                        contactsPerListing = TryGetDouble(kpis, "contactsPerListing", "ContactsPerListing", "avgContactsPerListing", "AvgContactsPerListing");
                        contactRate = TryGetDouble(kpis, "contactRate", "ContactRate", "conversionRate", "ConversionRate");
                    }

                    // Top vehicle — try multiple property name formats
                    var topVehicles = TryGetArrayProperty(report, "topVehicles", "TopVehicles", "topListings", "TopListings");
                    if (topVehicles.HasValue && topVehicles.Value.GetArrayLength() > 0)
                    {
                        var top = topVehicles.Value[0];
                        topVehicleTitle = TryGetString(top, "title", "Title", "name", "Name") ?? "—";
                        topVehicleViews = TryGetInt(top, "views", "Views", "viewCount", "ViewCount").ToString();
                        topVehicleContacts = TryGetInt(top, "contacts", "Contacts", "contactCount", "ContactCount").ToString();
                    }

                    // Market comparison — try multiple structures
                    if (report.TryGetProperty("marketComparison", out var market)
                        || report.TryGetProperty("MarketComparison", out market)
                        || report.TryGetProperty("benchmark", out market)
                        || report.TryGetProperty("Benchmark", out market))
                    {
                        marketAvgViews = TryGetDouble(market, "marketAvgViewsPerListing", "MarketAvgViewsPerListing",
                            "avgViews", "AvgViews", "averageViews", "AverageViews");
                        marketAvgContacts = TryGetDouble(market, "marketAvgContactsPerListing", "MarketAvgContactsPerListing",
                            "avgContacts", "AvgContacts", "averageContacts", "AverageContacts");
                        marketAvgContactRate = TryGetDouble(market, "marketAvgContactRate", "MarketAvgContactRate",
                            "avgContactRate", "AvgContactRate", "averageConversionRate", "AverageConversionRate");
                    }

                    _logger.LogInformation(
                        "Analytics fetched for DealerId={DealerId}: Views={Views}, Leads={Leads}, TopVehicle={TopVehicle}",
                        dealerId, totalViews, totalLeads, topVehicleTitle);
                }
                else
                {
                    _logger.LogWarning(
                        "DealerAnalyticsService returned {StatusCode} for dealer {DealerId} weekly report. Using defaults.",
                        response.StatusCode, dealerId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch analytics for DealerId={DealerId}. Sending report with available data.", dealerId);
        }

        // ── 3. Determine performance badge ─────────────────────────────────────
        var (badge, badgeColor, badgeMessage) = DeterminePerformanceBadge(
            viewsPerListing, marketAvgViews, contactRate, marketAvgContactRate);

        // ── 4. Render email template ────────────────────────────────────────────
        var templateParams = new Dictionary<string, object>
        {
            ["DealerName"] = dealerName ?? "Dealer",
            ["TotalViews"] = totalViews.ToString("N0"),
            ["TotalLeads"] = totalLeads.ToString("N0"),
            ["TopVehicleTitle"] = topVehicleTitle,
            ["TopVehicleViews"] = topVehicleViews,
            ["TopVehicleContacts"] = topVehicleContacts,
            ["ViewsPerListing"] = viewsPerListing.ToString("F1"),
            ["ContactsPerListing"] = contactsPerListing.ToString("F1"),
            ["ContactRate"] = contactRate.ToString("F1"),
            ["MarketAvgViews"] = marketAvgViews.ToString("F1"),
            ["MarketAvgContacts"] = marketAvgContacts.ToString("F1"),
            ["MarketAvgContactRate"] = marketAvgContactRate.ToString("F1"),
            ["PerformanceBadge"] = badge,
            ["PerformanceBadgeColor"] = badgeColor,
            ["PerformanceMessage"] = badgeMessage,
            ["UnsubscribeUrl"] = $"https://okla.com.do/configuracion/notificaciones?dealerId={dealerId}"
        };

        string emailBody;
        try
        {
            emailBody = await templateEngine.RenderTemplateAsync(TemplateFile, templateParams);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to render onboarding report template. Using inline fallback.");
            emailBody = BuildFallbackEmailHtml(templateParams);
        }

        // ── 5. Send email ───────────────────────────────────────────────────────
        try
        {
            var subject = $"📊 Tu primera semana en OKLA — {totalViews} vistas, {totalLeads} consultas";
            await emailService.SendEmailAsync(
                to: dealerEmail,
                subject: subject,
                body: emailBody,
                isHtml: true);

            _logger.LogInformation(
                "✅ 7-day onboarding report sent to {DealerEmail} for DealerId={DealerId}",
                dealerEmail, dealerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send onboarding report email to {DealerEmail}", dealerEmail);
        }

        // ── 6. WhatsApp notification (if phone available) ──────────────────────
        if (!string.IsNullOrWhiteSpace(dealerPhone))
        {
            try
            {
                var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
                await mediator.Send(new Application.UseCases.SendWhatsAppNotification.SendWhatsAppNotificationCommand(
                    To: NormalizePhoneNumber(dealerPhone),
                    Message: $"📊 *Reporte OKLA — 7 días*\n\n" +
                             $"Hola {dealerName ?? "dealer"}, tu primera semana:\n" +
                             $"👀 {totalViews} vistas\n" +
                             $"📩 {totalLeads} consultas\n" +
                             $"🏆 Más visto: {topVehicleTitle}\n\n" +
                             $"Ve tu dashboard completo: https://okla.com.do/dashboard/analytics"
                ), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WhatsApp onboarding report failed for DealerId={DealerId}. Non-critical.", dealerId);
            }
        }

        // ── 7. In-app notification ──────────────────────────────────────────────
        if (userNotifService != null && ownerUserId.HasValue)
        {
            try
            {
                await userNotifService.CreateAsync(
                    userId: ownerUserId.Value,
                    type: "onboarding_report",
                    title: "📊 Tu reporte de primera semana está listo",
                    message: $"{totalViews} vistas y {totalLeads} consultas en tus primeros 7 días",
                    icon: "📊",
                    link: "/dashboard/analytics",
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create in-app notification for onboarding report. Non-critical.");
            }
        }

        // ── 8. Admin alert ──────────────────────────────────────────────────────
        if (adminAlertService != null)
        {
            try
            {
                await adminAlertService.SendAlertAsync(
                    alertType: "dealer_onboarding_report_sent",
                    title: "Reporte de onboarding enviado",
                    message: $"7-day report sent to {dealerName} ({dealerEmail}): {totalViews} views, {totalLeads} leads",
                    severity: "Info",
                    metadata: new Dictionary<string, string>
                    {
                        ["DealerId"] = dealerId.ToString(),
                        ["DealerEmail"] = dealerEmail,
                        ["TotalViews"] = totalViews.ToString(),
                        ["TotalLeads"] = totalLeads.ToString(),
                        ["TopVehicle"] = topVehicleTitle
                    },
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send admin alert for onboarding report. Non-critical.");
            }
        }
    }

    private static (string Badge, string Color, string Message) DeterminePerformanceBadge(
        double viewsPerListing, double marketAvgViews,
        double contactRate, double marketAvgContactRate)
    {
        var viewsRatio = marketAvgViews > 0 ? viewsPerListing / marketAvgViews : 0;
        var contactRatio = marketAvgContactRate > 0 ? contactRate / marketAvgContactRate : 0;
        var avgRatio = (viewsRatio + contactRatio) / 2;

        return avgRatio switch
        {
            >= 1.5 => ("🌟 Rendimiento Excepcional", "#2e7d32", "¡Estás muy por encima del promedio del mercado!"),
            >= 1.0 => ("✅ Por Encima del Promedio", "#1565c0", "Tus publicaciones están rindiendo bien."),
            >= 0.7 => ("📈 Buen Comienzo", "#f57f17", "Estás cerca del promedio. ¡Sigue así!"),
            >= 0.3 => ("🚀 En Crecimiento", "#e65100", "Tu cuenta está despegando. Aplica los consejos para crecer."),
            _ => ("🌱 Comenzando", "#7b1fa2", "Es solo el inicio. Los mejores resultados vienen con más fotos y precios competitivos.")
        };
    }

    private static string NormalizePhoneNumber(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        // Dominican Republic area codes
        if (digits.Length == 10 && (digits.StartsWith("809") || digits.StartsWith("829") || digits.StartsWith("849")))
            return $"+1{digits}";
        // NANP with country code
        if (digits.Length == 11 && digits.StartsWith("1"))
            return $"+{digits}";
        // Already has international prefix
        if (phone.StartsWith("+"))
            return phone;
        // Fallback: prepend + for international format
        return $"+{digits}";
    }

    private static string BuildFallbackEmailHtml(Dictionary<string, object> p)
    {
        return $@"<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""UTF-8""></head>
<body style=""font-family: Arial, sans-serif; background-color: #f0f2f5; padding: 20px;"">
<div style=""max-width: 640px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);"">
<div style=""background: linear-gradient(135deg, #1a73e8, #0d47a1); padding: 32px 24px; text-align: center;"">
<h1 style=""color: white; margin: 0; font-size: 26px;"">📊 Tu Primera Semana en OKLA</h1>
</div>
<div style=""padding: 24px;"">
<p>Hola <strong>{p["DealerName"]}</strong>,</p>
<p>Aquí tu resumen de 7 días:</p>
<ul>
<li>👀 <strong>{p["TotalViews"]}</strong> vistas totales</li>
<li>📩 <strong>{p["TotalLeads"]}</strong> consultas recibidas</li>
<li>🏆 Vehículo más visto: <strong>{p["TopVehicleTitle"]}</strong></li>
</ul>
<p>Vistas por listing: {p["ViewsPerListing"]} vs. {p["MarketAvgViews"]} promedio del mercado</p>
<p>Consultas por listing: {p["ContactsPerListing"]} vs. {p["MarketAvgContacts"]} promedio del mercado</p>
<p>Tasa de contacto: {p["ContactRate"]}% vs. {p["MarketAvgContactRate"]}% promedio</p>
<p style=""text-align: center; margin-top: 20px;"">
<a href=""https://okla.com.do/dashboard/analytics"" style=""background-color: #1a73e8; color: white; padding: 14px 36px; text-decoration: none; border-radius: 6px; font-weight: bold;"">
Ver Dashboard Completo</a>
</p>
<p style=""color:#999;font-size:12px;text-align:center;margin-top:20px;"">OKLA — El marketplace automotriz #1 de RD</p>
</div>
</div>
</body>
</html>";
    }

    // ── JSON Helper Methods ──────────────────────────────────────────────────
    // These handle multiple property name formats (camelCase, PascalCase, aliases)
    // to support different API response shapes without breaking.

    private static int TryGetInt(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetInt32();
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var val))
                    return val;
            }
        }
        return 0;
    }

    private static double TryGetDouble(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetDouble();
                if (prop.ValueKind == JsonValueKind.String && double.TryParse(prop.GetString(), out var val))
                    return val;
            }
        }
        return 0;
    }

    private static string? TryGetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
        }
        return null;
    }

    private static JsonElement? TryGetArrayProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Array)
                return prop;
        }
        return null;
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? _configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"] ?? _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection($"DealerOnboardingReport-{Environment.MachineName}");
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            var queueArgs = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = $"{ExchangeName}.dlq",
                ["x-dead-letter-routing-key"] = $"{RoutingKey}.dlq"
            };

            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            _channel.QueueBind(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: RoutingKey);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ initialized for DealerOnboardingReportConsumer on queue: {Queue}", QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for DealerOnboardingReportConsumer");
            throw;
        }
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection for DealerOnboardingReportConsumer");
        }

        base.Dispose();
    }
}
