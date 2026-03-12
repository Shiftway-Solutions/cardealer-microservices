using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using UserService.Application.DTOs.Privacy;

namespace UserService.Application.UseCases.Privacy.GetUserDataSummary;

/// <summary>
/// Query para obtener el resumen de datos del usuario
/// </summary>
public record GetUserDataSummaryQuery(Guid UserId) : IRequest<UserDataSummaryDto>;

/// <summary>
/// Handler para GetUserDataSummaryQuery
/// </summary>
public class GetUserDataSummaryQueryHandler : IRequestHandler<GetUserDataSummaryQuery, UserDataSummaryDto>
{
    // TODO: Inyectar repositorios necesarios

    public async Task<UserDataSummaryDto> Handle(GetUserDataSummaryQuery request, CancellationToken cancellationToken)
    {
        // TODO: Implementar query real a la base de datos
        // Por ahora retornamos datos de ejemplo

        await Task.CompletedTask;

        return new UserDataSummaryDto(
            Profile: new ProfileSummaryDto(
                FullName: "Usuario Demo",
                Email: "demo@okla.com.do",
                Phone: "+18091234567",
                City: "Santo Domingo",
                Province: "Distrito Nacional",
                AccountType: "Buyer",
                MemberSince: DateTime.UtcNow.AddMonths(-6),
                EmailVerified: true
            ),
            Activity: new ActivitySummaryDto(
                TotalSearches: 45,
                TotalVehicleViews: 120,
                TotalFavorites: 8,
                TotalAlerts: 3,
                TotalMessages: 12,
                LastActivity: DateTime.UtcNow.AddHours(-2)
            ),
            Transactions: new TransactionsSummaryDto(
                TotalPayments: 2,
                TotalSpent: 5800.00m,
                ActiveSubscription: null,
                TotalInvoices: 2
            ),
            Privacy: new PrivacySettingsSummaryDto(
                MarketingOptIn: false,
                AnalyticsOptIn: true,
                ThirdPartyOptIn: false,
                LastUpdated: DateTime.UtcNow.AddDays(-30)
            ),
            GeneratedAt: DateTime.UtcNow
        );
    }
}
