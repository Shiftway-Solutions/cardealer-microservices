using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using UserService.Application.DTOs.Privacy;

namespace UserService.Application.UseCases.Privacy.GetUserFullData;

/// <summary>
/// Query para obtener todos los datos del usuario (Portabilidad ARCO)
/// </summary>
public record GetUserFullDataQuery(Guid UserId) : IRequest<UserFullDataDto>;

/// <summary>
/// Handler para GetUserFullDataQuery
/// </summary>
public class GetUserFullDataQueryHandler : IRequestHandler<GetUserFullDataQuery, UserFullDataDto>
{
    public async Task<UserFullDataDto> Handle(GetUserFullDataQuery request, CancellationToken cancellationToken)
    {
        // TODO: Implementar query real agregando datos de múltiples servicios
        await Task.CompletedTask;

        return new UserFullDataDto(
            ExportDate: DateTime.UtcNow,
            ExportVersion: "1.0",
            Profile: new UserProfileExportDto(
                Id: request.UserId,
                Email: "demo@okla.com.do",
                FirstName: "Juan",
                LastName: "Pérez",
                PhoneNumber: "+18091234567",
                ProfilePicture: null,
                City: "Santo Domingo",
                Province: "Distrito Nacional",
                BusinessName: null,
                BusinessPhone: null,
                BusinessAddress: null,
                RNC: null,
                AccountType: "Buyer",
                CreatedAt: DateTime.UtcNow.AddMonths(-6),
                LastLoginAt: DateTime.UtcNow.AddHours(-2),
                EmailVerified: true
            ),
            Activity: new UserActivityExportDto(
                SearchHistory: new List<SearchHistoryExportDto>
                {
                    new("Toyota Corolla 2020", "year:2020,make:Toyota", DateTime.UtcNow.AddDays(-5)),
                    new("SUV menos de 1M", "type:SUV,maxPrice:1000000", DateTime.UtcNow.AddDays(-10))
                },
                VehicleViews: new List<VehicleViewExportDto>
                {
                    new(Guid.NewGuid(), "Toyota Corolla 2020 - $950,000", DateTime.UtcNow.AddDays(-1)),
                    new(Guid.NewGuid(), "Honda Civic 2021 - $1,100,000", DateTime.UtcNow.AddDays(-3))
                },
                Favorites: new List<FavoriteExportDto>
                {
                    new(Guid.NewGuid(), "Toyota Corolla 2020", DateTime.UtcNow.AddDays(-2), "Buen precio"),
                    new(Guid.NewGuid(), "Honda CR-V 2019", DateTime.UtcNow.AddDays(-7), null)
                },
                Alerts: new List<AlertExportDto>
                {
                    new(Guid.NewGuid(), "PriceAlert", "vehicleId:xxx,targetPrice:900000", DateTime.UtcNow.AddDays(-5))
                },
                Sessions: new List<SessionExportDto>
                {
                    new("192.168.1.1", "Chrome/Windows", "Santo Domingo, DO", DateTime.UtcNow.AddHours(-2)),
                    new("192.168.1.2", "Safari/iPhone", "Santo Domingo, DO", DateTime.UtcNow.AddDays(-1))
                }
            ),
            Transactions: new UserTransactionsExportDto(
                Payments: new List<PaymentExportDto>
                {
                    new("pay_123", 2900.00m, "DOP", "Completed", "Card", DateTime.UtcNow.AddMonths(-2))
                },
                Invoices: new List<InvoiceExportDto>
                {
                    new("INV-001", 2900.00m, "Paid", DateTime.UtcNow.AddMonths(-2))
                },
                Subscriptions: new List<SubscriptionExportDto>()
            ),
            Messages: new UserMessagesExportDto(
                TotalConversations: 3,
                TotalMessages: 12,
                Conversations: new List<ConversationExportDto>
                {
                    new(Guid.NewGuid(), "Dealer XYZ", "Consulta sobre Toyota", 5, DateTime.UtcNow.AddDays(-2))
                }
            ),
            Settings: new UserSettingsExportDto(
                CommunicationPreferences: new CommunicationSettingsExportDto(
                    EmailActivityNotifications: true,
                    EmailListingUpdates: true,
                    EmailNewsletter: false,
                    EmailPromotions: false,
                    SmsAlerts: false,
                    SmsPromotions: false,
                    PushNewMessages: true,
                    PushPriceChanges: true,
                    PushRecommendations: false
                ),
                PrivacyPreferences: new PrivacySettingsExportDto(
                    AllowProfiling: true,
                    AllowThirdPartySharing: false,
                    AllowAnalytics: true,
                    AllowRetargeting: false
                )
            ),
            DataSharing: new DataSharingInfoDto(
                Description: "Información sobre terceros con los que se han compartido tus datos",
                ThirdParties: new List<ThirdPartyShareDto>
                {
                    new("Stripe (Procesador de Pagos)", "Procesamiento de pagos", "Nombre, Email", DateTime.UtcNow.AddMonths(-2)),
                    new("SendGrid (Email)", "Envío de notificaciones", "Email, Nombre", DateTime.UtcNow.AddMonths(-6))
                }
            )
        );
    }
}
