using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using UserService.Application.DTOs.Privacy;

namespace UserService.Application.UseCases.Privacy.GetPrivacyRequestHistory;

/// <summary>
/// Query para obtener historial de solicitudes ARCO
/// </summary>
public record GetPrivacyRequestHistoryQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 10
) : IRequest<PrivacyRequestsListDto>;

/// <summary>
/// Handler para GetPrivacyRequestHistoryQuery
/// </summary>
public class GetPrivacyRequestHistoryQueryHandler : IRequestHandler<GetPrivacyRequestHistoryQuery, PrivacyRequestsListDto>
{
    public async Task<PrivacyRequestsListDto> Handle(GetPrivacyRequestHistoryQuery request, CancellationToken cancellationToken)
    {
        // TODO: Implementar query real a la base de datos
        await Task.CompletedTask;

        var mockRequests = new List<PrivacyRequestHistoryDto>
        {
            new(
                Id: Guid.NewGuid(),
                Type: "Access",
                Status: "Completed",
                Description: "Solicitud de acceso a datos personales",
                CreatedAt: DateTime.UtcNow.AddDays(-10),
                CompletedAt: DateTime.UtcNow.AddDays(-8)
            ),
            new(
                Id: Guid.NewGuid(),
                Type: "Portability",
                Status: "Completed",
                Description: "Exportación de datos en formato JSON",
                CreatedAt: DateTime.UtcNow.AddDays(-30),
                CompletedAt: DateTime.UtcNow.AddDays(-29)
            ),
            new(
                Id: Guid.NewGuid(),
                Type: "Rectification",
                Status: "Completed",
                Description: "Actualización de dirección de correo",
                CreatedAt: DateTime.UtcNow.AddDays(-45),
                CompletedAt: DateTime.UtcNow.AddDays(-45)
            )
        };

        return new PrivacyRequestsListDto(
            Requests: mockRequests,
            TotalCount: 3,
            Page: request.Page,
            PageSize: request.PageSize
        );
    }
}
