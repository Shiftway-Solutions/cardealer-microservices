using AuthService.Application.DTOs.ExternalAuth;
using MediatR;

namespace AuthService.Application.Features.ExternalAuth.Commands.LinkExternalAccount;

public record LinkExternalAccountCommand(string UserId, string Provider, string IdToken) : IRequest<ExternalAuthResponse>;
