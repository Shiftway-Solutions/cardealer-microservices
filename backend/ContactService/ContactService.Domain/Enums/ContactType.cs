namespace ContactService.Domain.Enums;

/// <summary>
/// Type of contact inquiry.
/// </summary>
public enum ContactType
{
    /// <summary>General question about a vehicle.</summary>
    GeneralQuestion = 0,

    /// <summary>Request for more information.</summary>
    MoreInfo = 1,

    /// <summary>Price negotiation inquiry.</summary>
    PriceNegotiation = 2,

    /// <summary>Request to schedule a viewing or test drive.</summary>
    ScheduleViewing = 3,

    /// <summary>Financing question.</summary>
    FinancingQuestion = 4,

    /// <summary>Trade-in inquiry.</summary>
    TradeIn = 5
}
