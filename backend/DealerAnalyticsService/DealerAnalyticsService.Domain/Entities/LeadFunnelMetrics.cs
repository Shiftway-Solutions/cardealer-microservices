namespace DealerAnalyticsService.Domain.Entities;

/// <summary>
/// Métricas del funnel de conversión de leads
/// Desde impresiones hasta ventas cerradas
/// </summary>
public class LeadFunnelMetrics
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodType { get; set; } = "Daily"; // Daily, Weekly, Monthly

    // Funnel Stages
    public int Impressions { get; set; }       // Vistas en búsqueda
    public int Views { get; set; }             // Vistas de detalle
    public int Contacts { get; set; }          // Contactos iniciados
    public int Qualified { get; set; }         // Leads calificados
    public int Negotiation { get; set; }       // En negociación
    public int Converted { get; set; }         // Ventas cerradas

    // Stage Breakdown by Source
    public int OrganicImpressions { get; set; }
    public int PaidImpressions { get; set; }
    public int ReferralImpressions { get; set; }
    public int DirectViews { get; set; }
    public int SearchViews { get; set; }

    // Contact Type Breakdown
    public int PhoneContacts { get; set; }
    public int WhatsAppContacts { get; set; }
    public int EmailContacts { get; set; }
    public int ChatContacts { get; set; }

    // Lead Quality Distribution
    public int HotLeads { get; set; }
    public int WarmLeads { get; set; }
    public int ColdLeads { get; set; }

    // Timing Metrics
    public double AvgTimeToFirstContact { get; set; } // Hours
    public double AvgTimeToQualification { get; set; } // Hours
    public double AvgTimeToClose { get; set; } // Days

    // Conversion Rates (Stage to Stage)
    public double ImpressionsToViews => Impressions > 0
        ? (double)Views / Impressions * 100
        : 0;

    public double ViewsToContacts => Views > 0
        ? (double)Contacts / Views * 100
        : 0;

    public double ContactsToQualified => Contacts > 0
        ? (double)Qualified / Contacts * 100
        : 0;

    public double QualifiedToNegotiation => Qualified > 0
        ? (double)Negotiation / Qualified * 100
        : 0;

    public double NegotiationToConverted => Negotiation > 0
        ? (double)Converted / Negotiation * 100
        : 0;

    public double QualifiedToConverted => Qualified > 0
        ? (double)Converted / Qualified * 100
        : 0;

    public double OverallConversion => Impressions > 0
        ? (double)Converted / Impressions * 100
        : 0;

    // Revenue Attribution
    public decimal AttributedRevenue { get; set; }
    public decimal AvgDealValue { get; set; }
    public decimal CostPerLead { get; set; }
    public decimal CostPerAcquisition { get; set; }
    public decimal ReturnOnInvestment { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Constructor
    public LeadFunnelMetrics()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calcula el costo por lead si hay datos de inversión
    /// </summary>
    public void CalculateCostMetrics(decimal totalInvestment)
    {
        if (Qualified > 0)
            CostPerLead = totalInvestment / Qualified;

        if (Converted > 0)
            CostPerAcquisition = totalInvestment / Converted;

        if (totalInvestment > 0)
            ReturnOnInvestment = ((AttributedRevenue - totalInvestment) / totalInvestment) * 100;
    }
}

/// <summary>
/// Etapa del funnel para visualización
/// </summary>
public class FunnelStage
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public double ConversionRate { get; set; }
    public double DropOffRate { get; set; }
    public string Color { get; set; } = "#3B82F6";

    public static List<FunnelStage> FromMetrics(LeadFunnelMetrics metrics)
    {
        return new List<FunnelStage>
        {
            new() { Name = "Impresiones", Value = metrics.Impressions, Color = "#93C5FD" },
            new() { Name = "Vistas", Value = metrics.Views, ConversionRate = metrics.ImpressionsToViews, Color = "#60A5FA" },
            new() { Name = "Contactos", Value = metrics.Contacts, ConversionRate = metrics.ViewsToContacts, Color = "#3B82F6" },
            new() { Name = "Calificados", Value = metrics.Qualified, ConversionRate = metrics.ContactsToQualified, Color = "#2563EB" },
            new() { Name = "Negociación", Value = metrics.Negotiation, ConversionRate = metrics.QualifiedToNegotiation, Color = "#1D4ED8" },
            new() { Name = "Ventas", Value = metrics.Converted, ConversionRate = metrics.NegotiationToConverted, Color = "#1E40AF" }
        };
    }
}
