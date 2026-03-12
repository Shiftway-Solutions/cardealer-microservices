using AuthService.Application.DTOs.ExternalAuth;
using MediatR;

namespace AuthService.Application.Features.ExternalAuth.Commands.ExternalLogin;

public record ExternalLoginCommand(string Provider, string RedirectUri) : IRequest<ExternalLoginResponse>;
