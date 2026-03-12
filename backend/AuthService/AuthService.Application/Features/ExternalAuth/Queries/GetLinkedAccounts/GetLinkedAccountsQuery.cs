using AuthService.Application.DTOs.ExternalAuth;
using MediatR;

namespace AuthService.Application.Features.ExternalAuth.Queries.GetLinkedAccounts;

public record GetLinkedAccountsQuery(string UserId) : IRequest<List<LinkedAccountResponse>>;
